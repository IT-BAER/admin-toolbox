using System.Security;

namespace AdminToolbox.Models;

/// <summary>
/// Session-scoped, in-memory credential store.
/// Credentials are NEVER written to disk, registry, or any persistent storage.
/// The SecureString password is zeroed and disposed when the app exits or the user locks.
/// </summary>
public sealed class CredentialStore : IDisposable
{
    private static readonly Lazy<CredentialStore> _lazy = new(() => new CredentialStore());
    public static CredentialStore Instance => _lazy.Value;

    private SecureString? _password;
    private bool _disposed;

    private CredentialStore() { }

    /// <summary>Username (UPN like user@domain or SAM like DOMAIN\user).</summary>
    public string? Username { get; private set; }

    /// <summary>Domain component (null when stored as UPN).</summary>
    public string? Domain { get; private set; }

    /// <summary>Display label for the UI (full original input).</summary>
    public string? DisplayName { get; private set; }

    public bool HasCredentials => !_disposed && _password is not null;

    /// <summary>Store credentials. Any previously held password is zeroed first.</summary>
    public void Store(string username, string? domain, string displayName, SecureString password)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _password?.Dispose();
        Username = username;
        Domain = domain;
        DisplayName = displayName;
        _password = password.Copy();
        _password.MakeReadOnly();
    }

    /// <summary>Returns the stored SecureString. Caller must NOT dispose it.</summary>
    public SecureString GetPassword()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _password ?? throw new InvalidOperationException("No credentials are stored.");
    }

    /// <summary>Zero and discard all credentials (Lock action).</summary>
    public void Clear()
    {
        _password?.Dispose();
        _password = null;
        Username = null;
        Domain = null;
        DisplayName = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Clear();
        _disposed = true;
    }
}
