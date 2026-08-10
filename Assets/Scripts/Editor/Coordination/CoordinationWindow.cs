using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotionPanic.Editor.Coordination
{
  public sealed class UnityCoordinationWindowPathSource : ICoordinationWindowPathSource
  {
    public bool TryGetActiveStagePath(out string path)
    {
      var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
      path = prefabStage == null ? SceneManager.GetActiveScene().path : prefabStage.assetPath;
      return !string.IsNullOrEmpty(path);
    }

    public bool TryGetProjectSelectionPath(out string path)
    {
      path = Selection.activeObject == null
        ? string.Empty
        : AssetDatabase.GetAssetPath(Selection.activeObject);
      return !string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path);
    }
  }

  public sealed class UnityCoordinationOverrideConfirmation
    : ICoordinationOverrideConfirmation
  {
    public bool Confirm(string path, string owner)
    {
      return EditorUtility.DisplayDialog(
        "Override coordination claim?",
        path + " is claimed by " + owner
          + ". Override transfers the editing lease to this connection.",
        "Override",
        "Cancel");
    }
  }

  public sealed class UnityCoordinationWindowConfirmation
    : ICoordinationWindowConfirmation
  {
    public bool ConfirmManualMode(string message)
    {
      return EditorUtility.DisplayDialog("Enter Manual mode?", message,
        "Enter Manual mode", "Cancel");
    }

    public bool ConfirmReconciliation(string path, string message)
    {
      return EditorUtility.DisplayDialog("Mark warning reconciled?",
        path + "\n\n" + message, "Mark reconciled", "Cancel");
    }

    public bool ConfirmForgetCredentials(string message)
    {
      return EditorUtility.DisplayDialog("Forget developer credential?", message,
        "Forget credential", "Cancel");
    }
  }

  public sealed class CoordinationWindow : EditorWindow
  {
    private CoordinationWindowViewModel viewModel;
    private Vector2 scrollPosition;
    private bool showAdvancedPath;
    private bool showPresence = true;
    private bool showEditingLeases = true;
    private bool showReservations = true;
    private bool clearKeyboardFocus;

    [MenuItem("Window/Potion Panic/Coordination")]
    public static void ShowWindow()
    {
      var window = GetWindow<CoordinationWindow>();
      window.titleContent = new GUIContent("Coordination");
      window.minSize = new Vector2(430, 420);
      window.Show();
    }

    public static bool TryPublishNotification(CoordinationNotification notification)
    {
      if (notification == null || string.IsNullOrEmpty(notification.Message))
      {
        return true;
      }

      var published = false;
      foreach (var window in Resources.FindObjectsOfTypeAll<CoordinationWindow>())
      {
        window.ShowNotification(new GUIContent(notification.Message));
        window.Repaint();
        published = true;
      }
      return published;
    }

    private void OnEnable()
    {
      clearKeyboardFocus = true;
      CoordinationEditorBootstrap.ReconnectRuntime();
      CoordinationEditorBootstrap.FlushPendingNotifications();
      EditorApplication.update -= BindViewModel;
      EditorApplication.update += BindViewModel;
      BindViewModel();
    }

    private void OnDisable()
    {
      EditorApplication.update -= BindViewModel;
      if (viewModel != null)
      {
        viewModel.Changed -= Repaint;
        viewModel = null;
      }
    }

    private void BindViewModel()
    {
      var current = CoordinationEditorBootstrap.ViewModel;
      if (ReferenceEquals(viewModel, current))
      {
        return;
      }

      if (viewModel != null)
      {
        viewModel.Changed -= Repaint;
      }

      viewModel = current;
      clearKeyboardFocus = true;
      if (viewModel != null)
      {
        viewModel.Changed += Repaint;
      }
      Repaint();
    }

    private void OnGUI()
    {
      BindViewModel();
      if (clearKeyboardFocus)
      {
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
        clearKeyboardFocus = false;
      }

      if (viewModel == null)
      {
        EditorGUILayout.HelpBox(
          "Coordination is starting. Check the Console if this state persists.",
          MessageType.Info);
        return;
      }

      scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
      viewModel.RefreshActiveStage();
      DrawStatus();
      DrawWarnings();
      DrawActions();
      DrawRows("Presence", viewModel.Presence, "No coordinated assets are open.",
        ref showPresence);
      DrawRows("Editing leases", viewModel.EditingLeases, "No editing leases.",
        ref showEditingLeases);
      DrawRows("Reservations", viewModel.Reservations, "No reservations.",
        ref showReservations);
      EditorGUILayout.EndScrollView();
    }

    private void DrawStatus()
    {
      EditorGUILayout.LabelField("Coordination", EditorStyles.boldLabel);
      using (new EditorGUI.DisabledScope(!viewModel.CanEditDisabled))
      {
        EditorGUI.BeginChangeCheck();
        var nextMode = (CoordinationMode)EditorGUILayout.EnumPopup(
          new GUIContent("Mode", "Coordinated uses live team data. Manual saves require confirmation."),
          viewModel.Mode);
        if (EditorGUI.EndChangeCheck())
        {
          viewModel.SetMode(nextMode);
        }
      }
      DrawStatusValue("Identity", viewModel.Identity, Color.clear);
      DrawStatusValue("Git branch",
        string.IsNullOrEmpty(viewModel.Branch) ? "Unavailable" : viewModel.Branch,
        Color.clear);
      DrawStatusValue("Connection", viewModel.ConnectionLabel,
        ConnectionColor(viewModel.ConnectionState));
      DrawStatusValue("Team data", FreshnessLabel(viewModel.Freshness),
        FreshnessColor(viewModel.Freshness));

      var taskContextRect = EditorGUILayout.GetControlRect();
      var taskContextTextRect = EditorGUI.PrefixLabel(taskContextRect,
        new GUIContent("Task context"));
      var taskContext = GUI.TextField(
        taskContextTextRect,
        viewModel.TaskContext,
        CoordinationProtocol.MaximumContextLength);
      if (taskContext != viewModel.TaskContext)
      {
        viewModel.TaskContext = taskContext;
      }

      if (!viewModel.CanEditDisabled)
      {
        EditorGUILayout.HelpBox(
          "Coordination is available only in the Windows editor.",
          MessageType.Info);
      }
      EditorGUILayout.Space();
    }

    private void DrawWarnings()
    {
      var warnings = viewModel.OutstandingWarnings;
      if (!string.IsNullOrEmpty(viewModel.WarningStoreError))
      {
        EditorGUILayout.HelpBox(viewModel.WarningStoreError, MessageType.Error);
      }
      if (warnings.Count == 0)
      {
        return;
      }

      EditorGUILayout.LabelField("Save warnings", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "These records remain until each affected asset is reviewed and marked reconciled.",
        MessageType.Warning);
      foreach (var warning in warnings)
      {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
          var owner = string.IsNullOrEmpty(warning.LastKnownOwner)
            ? "unknown owner"
            : warning.LastKnownOwner;
          EditorGUILayout.LabelField(warning.Path, EditorStyles.wordWrappedLabel);
          DrawDetailValue("First save", EmptyFallback(warning.FirstSavedAtUtc));
          DrawDetailValue("Latest save", EmptyFallback(warning.LatestSavedAtUtc));
          DrawDetailValue("Save count", warning.SaveCount.ToString());
          DrawDetailValue("Reason", EmptyFallback(warning.Reason));
          DrawDetailValue("Last known owner", owner);
          DrawDetailValue("Branch", EmptyFallback(warning.Branch));
          DrawDetailValue("Task", EmptyFallback(warning.Task));
          if (!string.IsNullOrEmpty(warning.Error))
          {
            EditorGUILayout.HelpBox(warning.Error, MessageType.Error);
          }
          if (GUILayout.Button(new GUIContent("Mark reconciled",
            "Removes only this local warning after confirmation. It does not merge files or update server history.")))
          {
            viewModel.MarkReconciled(warning);
          }
        }
      }
      EditorGUILayout.Space();
    }

    private void DrawActions()
    {
      EditorGUILayout.LabelField("Current asset", EditorStyles.boldLabel);
      EditorGUILayout.LabelField(
        string.IsNullOrEmpty(viewModel.SelectedPath)
          ? "No saved active asset"
          : viewModel.SelectedPath,
        EditorStyles.wordWrappedLabel);
      EditorGUILayout.LabelField("Source", TargetSourceLabel(viewModel.TargetSource));
      EditorGUILayout.HelpBox(viewModel.TargetHelpText, MessageType.Info);

      var primary = viewModel.PrimaryAction;
      using (new EditorGUI.DisabledScope(primary == CoordinationPrimaryAction.None))
      {
        if (GUILayout.Button(new GUIContent(PrimaryActionLabel(primary),
          PrimaryActionTooltip(primary)), GUILayout.ExpandWidth(true)))
        {
          viewModel.PerformPrimaryAction();
        }
      }

      var compact = position.width < 560;
      if (compact)
      {
        DrawButton("Follow active stage", "Use the saved active Scene or Prefab Stage.",
          true, viewModel.FollowActiveStage);
        DrawButton("Use Project selection", "Use the selected Project asset as the current target.",
          true, viewModel.UseProjectSelection);
      }
      else
      {
        using (new EditorGUILayout.HorizontalScope())
        {
          DrawButton("Follow active stage", "Use the saved active Scene or Prefab Stage.",
            true, viewModel.FollowActiveStage);
          DrawButton("Use Project selection", "Use the selected Project asset as the current target.",
            true, viewModel.UseProjectSelection);
        }
      }

      showAdvancedPath = EditorGUILayout.Foldout(
        showAdvancedPath, "Advanced path", true);
      if (showAdvancedPath)
      {
        EditorGUI.BeginChangeCheck();
        var manualPath = EditorGUILayout.TextField(
          "Asset path", viewModel.SelectedPath);
        if (EditorGUI.EndChangeCheck())
        {
          viewModel.SelectedPath = manualPath;
        }
      }

      if (compact)
      {
        DrawSecondaryActionsVertical();
      }
      else
      {
        DrawSecondaryActionsHorizontal();
      }
      EditorGUILayout.Space();
    }

    private void DrawSecondaryActionsHorizontal()
    {
      using (new EditorGUILayout.HorizontalScope())
      {
        DrawButton("Copy path", "Copy the canonical current-asset path.", viewModel.CanCopyCanonicalPath,
          viewModel.CopyCanonicalPath);
        DrawButton("Reconnect", "Reconnect to retrieve current team data.",
          viewModel.CanReconnect, viewModel.Reconnect);
        DrawButton("Override…", "Request an override for the current remote claim.",
          viewModel.CanOverride, viewModel.Override);
        DrawButton("Forget credentials", "Delete the saved developer credential after confirmation.",
          viewModel.CanForgetCredentials,
          viewModel.ForgetCredentials);
      }
    }

    private void DrawSecondaryActionsVertical()
    {
      DrawButton("Copy path", "Copy the canonical current-asset path.",
        viewModel.CanCopyCanonicalPath, viewModel.CopyCanonicalPath);
      DrawButton("Reconnect", "Reconnect to retrieve current team data.",
        viewModel.CanReconnect, viewModel.Reconnect);
      DrawButton("Override…", "Request an override for the current remote claim.",
        viewModel.CanOverride, viewModel.Override);
      DrawButton("Forget credentials", "Delete the saved developer credential after confirmation.",
        viewModel.CanForgetCredentials,
        viewModel.ForgetCredentials);
    }

    private static void DrawButton(
      string label,
      string tooltip,
      bool enabled,
      Func<bool> action)
    {
      using (new EditorGUI.DisabledScope(!enabled))
      {
        if (GUILayout.Button(new GUIContent(label, tooltip)))
        {
          action();
        }
      }
    }

    private void DrawRows(
      string title,
      IReadOnlyList<CoordinationWindowRow> rows,
      string emptyMessage,
      ref bool expanded)
    {
      expanded = EditorGUILayout.Foldout(expanded, title, true);
      if (!expanded)
      {
        return;
      }
      if (rows.Count == 0)
      {
        EditorGUILayout.LabelField(emptyMessage, EditorStyles.miniLabel);
        EditorGUILayout.Space();
        return;
      }

      foreach (var row in rows)
      {
        var isSelected = viewModel.IsExpanded(row);
        var style = new GUIStyle(EditorStyles.helpBox);
        if (isSelected)
        {
          style.normal.background = EditorStyles.selectionRect.normal.background;
        }

        using (var rowScope = new EditorGUILayout.VerticalScope(style))
        {
          var rowExpanded = EditorGUILayout.Foldout(isSelected,
            new GUIContent(RowDetailsLabel(row),
              "Show or hide this team's claim details and available actions."), true);
          EditorGUILayout.LabelField(row.Path, EditorStyles.wordWrappedLabel);
          if (rowExpanded != isSelected)
          {
            viewModel.SelectRow(row);
          }
          if (isSelected)
          {
            EditorGUILayout.LabelField("Details", EditorStyles.miniLabel);
            DrawDetailValue("Owner",
              row.Owner + (row.IsLocal ? " (local)" : string.Empty));
            DrawDetailValue("Developer ID", row.DeveloperId);
            DrawDetailValue("Branch", EmptyFallback(row.Branch));
            DrawDetailValue("Task", EmptyFallback(row.Task));
            DrawDetailValue("Expires", EmptyFallback(row.ExpiresAt));
            if (row.Kind == CoordinationWindowRowKind.EditingLease && row.IsLocal)
            {
              DrawButton("Release editing lease", "Release this local editing lease.",
                viewModel.CanReleaseRow(row),
                () => viewModel.Release(row));
            }
            else if (row.Kind == CoordinationWindowRowKind.Reservation && row.IsLocal)
            {
              DrawButton("Cancel reservation", "Cancel this local reservation.",
                viewModel.CanCancelReservationRow(row),
                () => viewModel.CancelReservation(row));
            }
            else if (row.Kind != CoordinationWindowRowKind.Presence)
            {
              DrawButton("Override…", "Request an override for this remote claim.",
                viewModel.CanOverrideRow(row),
                () => viewModel.Override(row));
            }
            DrawButton("Copy path", "Copy this row's canonical asset path.",
              true, () => viewModel.CopyPath(row));
          }
        }
      }
      EditorGUILayout.Space();
    }

    private static string EmptyFallback(string value)
    {
      return string.IsNullOrEmpty(value) ? "None" : value;
    }

    private void DrawStatusValue(string label, string value, Color color)
    {
      EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
      using (new EditorGUILayout.HorizontalScope())
      {
        if (color != Color.clear)
        {
          var rect = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight,
            GUILayout.Width(10));
          EditorGUI.DrawRect(new Rect(rect.x, rect.y + 4, 8, 8), color);
        }
        EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
      }
    }

    private static void DrawDetailValue(string label, string value)
    {
      EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
      EditorGUILayout.LabelField(value, EditorStyles.wordWrappedLabel);
    }

    private static Color ConnectionColor(CoordinationConnectionState state)
    {
      return state == CoordinationConnectionState.Connected
        ? new Color(0.25f, 0.72f, 0.35f)
        : new Color(0.86f, 0.58f, 0.2f);
    }

    private static Color FreshnessColor(CoordinationDataFreshness freshness)
    {
      return freshness == CoordinationDataFreshness.Live
        ? new Color(0.25f, 0.72f, 0.35f)
        : freshness == CoordinationDataFreshness.WaitingForSnapshot
          ? new Color(0.86f, 0.58f, 0.2f)
          : new Color(0.82f, 0.32f, 0.28f);
    }

    private static string RowDetailsLabel(CoordinationWindowRow row)
    {
      return row.Kind == CoordinationWindowRowKind.Presence
        ? "Presence details"
        : row.Kind == CoordinationWindowRowKind.EditingLease
          ? "Editing lease details"
          : "Reservation details";
    }

    private static string FreshnessLabel(CoordinationDataFreshness freshness)
    {
      switch (freshness)
      {
        case CoordinationDataFreshness.WaitingForSnapshot:
          return "Waiting for team data";
        case CoordinationDataFreshness.Live:
          return "Live";
        case CoordinationDataFreshness.Stale:
          return "Last-known data, read-only";
        default:
          return "Team data unavailable";
      }
    }

    private static string TargetSourceLabel(CoordinationTargetSource source)
    {
      switch (source)
      {
        case CoordinationTargetSource.ProjectSelection:
          return "Project selection";
        case CoordinationTargetSource.ManualPath:
          return "Manual path";
        default:
          return "Active stage";
      }
    }

    private static string PrimaryActionLabel(CoordinationPrimaryAction action)
    {
      switch (action)
      {
        case CoordinationPrimaryAction.Reserve:
          return "Reserve";
        case CoordinationPrimaryAction.ReleaseEditingLease:
          return "Release editing lease";
        case CoordinationPrimaryAction.CancelReservation:
          return "Cancel reservation";
        default:
          return "No claim action available";
      }
    }

    private static string PrimaryActionTooltip(CoordinationPrimaryAction action)
    {
      switch (action)
      {
        case CoordinationPrimaryAction.Reserve:
          return "Reserve the current asset for editing.";
        case CoordinationPrimaryAction.ReleaseEditingLease:
          return "Release this connection's editing lease for the current asset.";
        case CoordinationPrimaryAction.CancelReservation:
          return "Cancel your reservation for the current asset.";
        default:
          return "Claim actions require current, live coordinated team data.";
      }
    }
  }
}
