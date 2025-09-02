using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tiwintza.Presentation.Wpf.Services.Windows;

public sealed class WindowsCredentialStorage : ICredentialStorage
{
    private const string TargetName = "Tiwintza/RememberMe";
    private const CredType Type = CredType.Generic;

    public Task SaveAsync(RememberMeCredential cred, CancellationToken ct = default)
    {
        // Serializamos username (+ token si existiera)
        var json = JsonSerializer.Serialize(cred);
        var bytes = Encoding.Unicode.GetBytes(json);

        // Nota: para GENÉRICOS, el límite práctico es ~512 bytes. Esto es pequeñísimo, así que OK.
        var credential = new CREDENTIAL
        {
            TargetName = TargetName,
            Type = Type,
            Persist = CredPersist.LocalMachine,
            AttributeCount = 0,
            UserName = cred.Username,
            CredentialBlobSize = (uint)bytes.Length
        };

        // Reservamos y copiamos el blob
        credential.CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);

            if (!CredWrite(ref credential, 0))
                throw new InvalidOperationException($"CredWrite failed: {Marshal.GetLastWin32Error()}");

            return Task.CompletedTask;
        }
        finally
        {
            // Limpieza de memoria
            if (credential.CredentialBlob != IntPtr.Zero)
                Marshal.FreeCoTaskMem(credential.CredentialBlob);
        }
    }

    public Task<RememberMeCredential?> LoadAsync(CancellationToken ct = default)
    {
        if (!CredRead(TargetName, Type, 0, out var pCredential) || pCredential == IntPtr.Zero)
            return Task.FromResult<RememberMeCredential?>(null);

        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(pCredential)!;
            if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize == 0)
                return Task.FromResult<RememberMeCredential?>(null);

            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
            var json = Encoding.Unicode.GetString(bytes);
            var parsed = JsonSerializer.Deserialize<RememberMeCredential>(json);
            return Task.FromResult(parsed);
        }
        finally
        {
            CredFree(pCredential);
        }
    }

    public Task DeleteAsync(CancellationToken ct = default)
    {
        // Si no existe, devuelve false; lo consideramos éxito “idempotente”.
        CredDelete(TargetName, Type, 0);
        return Task.CompletedTask;
    }

    #region P/Invoke

    private enum CredType : uint
    {
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4,
        GenericCertificate = 5,
        DomainExtended = 6,
        Maximum = 7,          // not a type
        MaximumEx = Maximum + 1000 // not a type
    }

    private enum CredPersist : uint
    {
        Session = 1,
        LocalMachine = 2,
        Enterprise = 3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CredType Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob; // LPBYTE
        public CredPersist Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite([In] ref CREDENTIAL Credential, [In] uint Flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, CredType type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, CredType type, int flags);

    #endregion
}
