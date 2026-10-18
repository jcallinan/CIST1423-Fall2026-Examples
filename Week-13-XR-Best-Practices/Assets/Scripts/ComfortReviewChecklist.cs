using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum ComfortCheckStatus
{
    NotRun,
    Pass,
    NeedsWork,
    NotApplicable
}

[Serializable]
public sealed class ComfortReviewCheck
{
    [SerializeField] private string id;
    [SerializeField, TextArea(2, 4)] private string prompt;
    [SerializeField] private ComfortCheckStatus status;
    [SerializeField, TextArea(2, 4)] private string evidence;

    public string Id => id;
    public string Prompt => prompt;
    public ComfortCheckStatus Status => status;
    public string Evidence => evidence;

    public ComfortReviewCheck()
    {
        id = string.Empty;
        prompt = string.Empty;
        status = ComfortCheckStatus.NotRun;
        evidence = string.Empty;
    }

    public ComfortReviewCheck(string id, string prompt)
    {
        this.id = id;
        this.prompt = prompt;
        status = ComfortCheckStatus.NotRun;
        evidence = string.Empty;
    }
}

public sealed class ComfortReviewChecklist : MonoBehaviour
{
    [SerializeField] private string sceneUnderReview;
    [SerializeField] private string deviceAndRuntime;
    [SerializeField] private List<ComfortReviewCheck> checks = new List<ComfortReviewCheck>();

    private void Reset()
    {
        RestoreStarterChecks();
    }

    [ContextMenu("Restore starter checks")]
    private void RestoreStarterChecks()
    {
        checks = new List<ComfortReviewCheck>
        {
            new ComfortReviewCheck("LOC-01", "Can the user understand, start, and stop the available locomotion and turning options?"),
            new ComfortReviewCheck("CAM-01", "Do camera motion and transitions avoid unexpected movement or orientation changes?"),
            new ComfortReviewCheck("REACH-01", "Can common tasks be completed from the intended standing or seated posture?"),
            new ComfortReviewCheck("UI-01", "Are important labels readable at their use distance, with clear non-audio-only feedback?"),
            new ComfortReviewCheck("ACCESS-01", "Are promised controls, comfort options, and access paths discoverable and testable?"),
            new ComfortReviewCheck("RECOVER-01", "Can the user pause, exit, or recover from an interrupted interaction?"),
            new ComfortReviewCheck("PERF-01", "Was representative scene load tested against a target selected for this device and runtime?")
        };
    }

    [ContextMenu("Print review report")]
    private void PrintReviewReport()
    {
        var report = BuildReviewReport();
        Debug.Log(report, this);
    }

    public string BuildReviewReport()
    {
        var counts = new int[Enum.GetValues(typeof(ComfortCheckStatus)).Length];
        var report = new StringBuilder();
        report.AppendLine("XR comfort review");
        report.AppendLine("Scene: " + (string.IsNullOrWhiteSpace(sceneUnderReview) ? "(not recorded)" : sceneUnderReview));
        report.AppendLine("Device/runtime: " + (string.IsNullOrWhiteSpace(deviceAndRuntime) ? "(not recorded)" : deviceAndRuntime));
        report.AppendLine();

        foreach (var check in checks)
        {
            counts[(int)check.Status]++;
            report.Append('[').Append(check.Status).Append("] ")
                .Append(check.Id).Append(" - ").AppendLine(check.Prompt);

            if (!string.IsNullOrWhiteSpace(check.Evidence))
            {
                report.Append("  Evidence: ").AppendLine(check.Evidence);
            }
        }

        report.AppendLine();
        report.Append("Summary: ")
            .Append("Pass ").Append(counts[(int)ComfortCheckStatus.Pass]).Append(", ")
            .Append("Needs Work ").Append(counts[(int)ComfortCheckStatus.NeedsWork]).Append(", ")
            .Append("Not Applicable ").Append(counts[(int)ComfortCheckStatus.NotApplicable]).Append(", ")
            .Append("Not Run ").Append(counts[(int)ComfortCheckStatus.NotRun]);

        return report.ToString();
    }
}