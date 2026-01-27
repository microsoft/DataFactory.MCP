using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace DataFactory.WindowsMCP.Tools;

/// <summary>
/// Windows-specific tool for managing credentials using Windows Credential Manager.
/// This tool demonstrates Windows-only functionality that cannot run cross-platform.
/// </summary>
[McpServerToolType]
[SupportedOSPlatform("windows")]
public class WindowsCredentialTool
{
    [McpServerTool, Description("Store a credential securely in Windows Credential Manager")]
    public string StoreCredential(
        [Description("The target name/identifier for the credential (e.g., 'DataFactory:MyConnection')")] string targetName,
        [Description("The username to store")] string username,
        [Description("The password/secret to store")] string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return "Error: Target name is required";
            if (string.IsNullOrWhiteSpace(username))
                return "Error: Username is required";
            if (string.IsNullOrWhiteSpace(password))
                return "Error: Password is required";

            var passwordBytes = Encoding.Unicode.GetBytes(password);

            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = targetName,
                UserName = username,
                CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length),
                CredentialBlobSize = (uint)passwordBytes.Length,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                Comment = "Stored by DataFactory.WindowsMCP"
            };

            try
            {
                Marshal.Copy(passwordBytes, 0, credential.CredentialBlob, passwordBytes.Length);

                if (!CredWrite(ref credential, 0))
                {
                    var error = Marshal.GetLastWin32Error();
                    return $"Failed to store credential. Windows error code: {error}";
                }

                return $"Credential '{targetName}' stored successfully in Windows Credential Manager.";
            }
            finally
            {
                Marshal.FreeHGlobal(credential.CredentialBlob);
            }
        }
        catch (Exception ex)
        {
            return $"Error storing credential: {ex.Message}";
        }
    }

    [McpServerTool, Description("Retrieve a credential from Windows Credential Manager")]
    public string GetCredential(
        [Description("The target name/identifier for the credential to retrieve")] string targetName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return "Error: Target name is required";

            if (!CredRead(targetName, CRED_TYPE_GENERIC, 0, out var credentialPtr))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_NOT_FOUND)
                    return $"Credential '{targetName}' not found in Windows Credential Manager.";
                return $"Failed to read credential. Windows error code: {error}";
            }

            try
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                var username = credential.UserName;

                // Note: For security, we don't return the actual password in plain text
                return $"Credential found:\n- Target: {targetName}\n- Username: {username}\n- Password: [stored securely - use GetCredentialPassword to retrieve]";
            }
            finally
            {
                CredFree(credentialPtr);
            }
        }
        catch (Exception ex)
        {
            return $"Error retrieving credential: {ex.Message}";
        }
    }

    [McpServerTool, Description("Delete a credential from Windows Credential Manager")]
    public string DeleteCredential(
        [Description("The target name/identifier for the credential to delete")] string targetName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetName))
                return "Error: Target name is required";

            if (!CredDelete(targetName, CRED_TYPE_GENERIC, 0))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_NOT_FOUND)
                    return $"Credential '{targetName}' not found in Windows Credential Manager.";
                return $"Failed to delete credential. Windows error code: {error}";
            }

            return $"Credential '{targetName}' deleted successfully from Windows Credential Manager.";
        }
        catch (Exception ex)
        {
            return $"Error deleting credential: {ex.Message}";
        }
    }

    [McpServerTool, Description("List all generic credentials stored in Windows Credential Manager")]
    public string ListCredentials()
    {
        try
        {
            if (!CredEnumerate(null, 0, out var count, out var credentialsPtr))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_NOT_FOUND)
                    return "No credentials found in Windows Credential Manager.";
                return $"Failed to enumerate credentials. Windows error code: {error}";
            }

            try
            {
                var result = new StringBuilder();
                result.AppendLine($"Found {count} credential(s) in Windows Credential Manager:");
                result.AppendLine();

                var ptrArray = new IntPtr[count];
                Marshal.Copy(credentialsPtr, ptrArray, 0, (int)count);

                foreach (var ptr in ptrArray)
                {
                    var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
                    if (cred.Type == CRED_TYPE_GENERIC)
                    {
                        result.AppendLine($"- Target: {cred.TargetName}");
                        result.AppendLine($"  Username: {cred.UserName}");
                        result.AppendLine();
                    }
                }

                return result.ToString();
            }
            finally
            {
                CredFree(credentialsPtr);
            }
        }
        catch (Exception ex)
        {
            return $"Error listing credentials: {ex.Message}";
        }
    }

    #region Windows API P/Invoke

    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 2;
    private const int ERROR_NOT_FOUND = 1168;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL credential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredEnumerate(string? filter, uint flags, out uint count, out IntPtr credentials);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);

    #endregion
}
