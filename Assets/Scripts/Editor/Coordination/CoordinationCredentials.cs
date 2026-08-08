using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PotionPanic.Editor.Coordination
{
  public interface ICredentialStore
  {
    bool TryRead(string target, out string value);
    void Write(string target, string value);
    void Delete(string target);
  }

  public static class CoordinationCredentialStore
  {
    public static string GetDeveloperTokenTarget(string projectId)
    {
      if (string.IsNullOrWhiteSpace(projectId))
      {
        throw new ArgumentException("Project ID is required.", nameof(projectId));
      }

      return "PotionPanic/Coordination/" + projectId + "/developer-token";
    }
  }

  public sealed class MemoryCredentialStore : ICredentialStore
  {
    private readonly Dictionary<string, string> values = new Dictionary<string, string>();

    public bool TryRead(string target, out string value)
    {
      return values.TryGetValue(target, out value);
    }

    public void Write(string target, string value)
    {
      if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(value))
      {
        throw new ArgumentException("Credential target and value are required.");
      }

      values[target] = value;
    }

    public void Delete(string target)
    {
      values.Remove(target);
    }
  }

  public sealed class WindowsCredentialStore : ICredentialStore
  {
    private const uint GenericCredential = 1;
    private const uint LocalMachinePersistence = 2;
    private const int NotFoundError = 1168;

    public bool TryRead(string target, out string value)
    {
      value = null;
#if UNITY_EDITOR_WIN
      if (!CredRead(target, GenericCredential, 0, out var credential))
      {
        var error = Marshal.GetLastWin32Error();
        if (error == NotFoundError)
        {
          return false;
        }

        throw new InvalidOperationException(
          "Windows Credential Manager could not read the developer token (" + error + ").");
      }

      try
      {
        var native = Marshal.PtrToStructure<NativeCredential>(credential);
        value = native.CredentialBlobSize == 0
          ? string.Empty
          : Marshal.PtrToStringUni(native.CredentialBlob, (int)native.CredentialBlobSize / 2);
        return !string.IsNullOrWhiteSpace(value);
      }
      finally
      {
        CredFree(credential);
      }
#else
      return false;
#endif
    }

    public void Write(string target, string value)
    {
      if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(value))
      {
        throw new ArgumentException("Credential target and value are required.");
      }

#if UNITY_EDITOR_WIN
      var bytes = Encoding.Unicode.GetBytes(value);
      var blob = Marshal.AllocCoTaskMem(bytes.Length);
      try
      {
        Marshal.Copy(bytes, 0, blob, bytes.Length);
        var credential = new NativeCredential
        {
          Type = GenericCredential,
          TargetName = target,
          CredentialBlobSize = (uint)bytes.Length,
          CredentialBlob = blob,
          Persist = LocalMachinePersistence,
          UserName = "PotionPanic"
        };
        if (!CredWrite(ref credential, 0))
        {
          throw new InvalidOperationException("Windows Credential Manager rejected the token.");
        }
      }
      finally
      {
        Marshal.FreeCoTaskMem(blob);
      }
#else
      throw new PlatformNotSupportedException("Windows Credential Manager is required.");
#endif
    }

    public void Delete(string target)
    {
#if UNITY_EDITOR_WIN
      if (!CredDelete(target, GenericCredential, 0)
        && Marshal.GetLastWin32Error() != NotFoundError)
      {
        throw new InvalidOperationException("Windows Credential Manager could not forget the token.");
      }
#endif
    }

#if UNITY_EDITOR_WIN
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
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

    [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(
      string target,
      uint type,
      int flags,
      out IntPtr credential);

    [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref NativeCredential credential, int flags);

    [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, int flags);

    [DllImport("advapi32", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);
#endif
  }
}
