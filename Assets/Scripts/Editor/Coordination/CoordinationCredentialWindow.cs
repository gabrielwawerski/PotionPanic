using System;
using UnityEditor;
using UnityEngine;

namespace PotionPanic.Editor.Coordination
{
  public sealed class CoordinationCredentialWindow : EditorWindow
  {
    private ICredentialStore credentialStore;
    private string credentialTarget;
    private string developerToken = string.Empty;
    private Action credentialsSaved;

    public static void ShowForProject(string projectId, ICredentialStore credentialStore,
      Action credentialsSaved)
    {
      var window = GetWindow<CoordinationCredentialWindow>(true, "Coordination Credentials");
      window.credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
      window.credentialTarget = CoordinationCredentialStore.GetDeveloperTokenTarget(projectId);
      window.credentialsSaved = credentialsSaved;
      window.developerToken = string.Empty;
      window.minSize = new Vector2(420, 120);
      window.ShowUtility();
    }

    public static bool TrySubmitToken(ICredentialStore credentialStore, string credentialTarget,
      string developerToken)
    {
      return TrySubmitToken(credentialStore, credentialTarget, developerToken, null);
    }

    public static bool TrySubmitToken(ICredentialStore credentialStore, string credentialTarget,
      string developerToken, Action credentialsSaved)
    {
      if (credentialStore == null || string.IsNullOrWhiteSpace(credentialTarget)
        || string.IsNullOrWhiteSpace(developerToken))
      {
        return false;
      }

      credentialStore.Write(credentialTarget, developerToken.Trim());
      credentialsSaved?.Invoke();
      return true;
    }

    private void OnDisable()
    {
      developerToken = string.Empty;
    }

    private void OnGUI()
    {
      EditorGUILayout.LabelField("Developer token", EditorStyles.boldLabel);
      developerToken = EditorGUILayout.PasswordField("Token", developerToken);
      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("Save") && TrySubmitToken(credentialStore, credentialTarget,
          developerToken, credentialsSaved))
        {
          developerToken = string.Empty;
          Close();
        }

        if (GUILayout.Button("Forget") && credentialStore != null
          && !string.IsNullOrWhiteSpace(credentialTarget))
        {
          credentialStore.Delete(credentialTarget);
          developerToken = string.Empty;
          Close();
        }
      }
    }
  }
}
