﻿using System;
using System.Collections.Generic;
using CodeRefactor.Commands;
using PluginCore;
using PluginCore.FRService;
using PluginCore.Managers;

namespace CodeRefactor.Provider
{
    internal class MovingHelper
    {
        static readonly List<QueueItem> queue = new List<QueueItem>();
        static readonly Dictionary<string, List<SearchMatch>> results = new Dictionary<string, List<SearchMatch>>();
        static RefactorCommand<IDictionary<string, List<SearchMatch>>> currentCommand;

        public static void AddToQueue(Dictionary<string, string> oldPathToNewPath) => AddToQueue(oldPathToNewPath, false);

        public static void AddToQueue(Dictionary<string, string> oldPathToNewPath, bool outputResults) => AddToQueue(oldPathToNewPath, outputResults, false);

        public static void AddToQueue(Dictionary<string, string> oldPathToNewPath, bool outputResults, bool renaming) => AddToQueue(oldPathToNewPath, outputResults, renaming, false);

        public static void AddToQueue(Dictionary<string, string> oldPathToNewPath, bool outputResults, bool renaming, bool updatePackages)
        {
            queue.Add(new QueueItem(oldPathToNewPath, outputResults, renaming, updatePackages));
            if (currentCommand is null) ExecuteFirst();
        }

        static void ExecuteFirst()
        {
            try
            {
                var item = queue[0];
                queue.Remove(item);
                currentCommand = CommandFactoryProvider.DefaultFactory.CreateMoveCommand(item.OldPathToNewPath, item.OutputResults, item.Renaming, item.UpdatePackages);
                currentCommand.OnRefactorComplete += OnRefactorComplete;
                currentCommand.Execute();
            }
            catch(Exception ex)
            {
                queue.Clear();
                results.Clear();
                currentCommand = null;
                ErrorManager.ShowError(ex);
            }
        }

        static void OnRefactorComplete(object sender, RefactorCompleteEventArgs<IDictionary<string, List<SearchMatch>>> e)
        {
            if (currentCommand.OutputResults)
            {
                foreach (var entry in currentCommand.Results)
                {
                    var path = entry.Key;
                    if (!results.ContainsKey(path)) results[path] = new List<SearchMatch>();
                    results[path].AddRange(entry.Value);
                }
            }
            if (queue.Count > 0) ExecuteFirst();
            else
            {
                if (results.Count > 0) ReportResults();
                results.Clear();
                currentCommand = null;
            }
        }

        static void ReportResults()
        {
            PluginBase.MainForm.CallCommand("PluginCommand", "ResultsPanel.ClearResults;" + PluginMain.TraceGroup);
            foreach (var entry in results)
            {
                var lineOffsets = new Dictionary<int, int>();
                var lineChanges = new Dictionary<int, string>();
                var reportableLines = new Dictionary<int, List<string>>();
                foreach (var match in entry.Value)
                {
                    var column = match.Column;
                    var lineNumber = match.Line;
                    var changedLine = lineChanges.TryGetValue(lineNumber, out var newChangedLine) ? newChangedLine : match.LineText;
                    lineOffsets.TryGetValue(lineNumber, out var offset);
                    column += offset;
                    lineChanges[lineNumber] = changedLine;
                    lineOffsets[lineNumber] = offset + (match.Value.Length - match.Length);
                    if (!reportableLines.ContainsKey(lineNumber)) reportableLines[lineNumber] = new List<string>();
                    reportableLines[lineNumber].Add(entry.Key + ":" + match.Line + ": chars " + column + "-" + (column + match.Value.Length) + " : {0}");
                }
                foreach (var lineSetsToReport in reportableLines)
                {
                    var line = lineChanges[lineSetsToReport.Key].Trim();
                    foreach (var lineToReport in lineSetsToReport.Value)
                    {
                        TraceManager.Add(string.Format(lineToReport, line), (int)TraceType.Info, PluginMain.TraceGroup);
                    }
                }
            }
            PluginBase.MainForm.CallCommand("PluginCommand", "ResultsPanel.ShowResults;" + PluginMain.TraceGroup);
        }
    }

    #region Helpers

    internal class QueueItem
    {
        public readonly Dictionary<string, string> OldPathToNewPath;
        public readonly bool OutputResults;
        public readonly bool Renaming;
        public readonly bool UpdatePackages;

        public QueueItem(Dictionary<string, string> oldPathToNewPath, bool outputResults, bool renaming, bool updatePackages)
        {
            OldPathToNewPath = oldPathToNewPath;
            OutputResults = outputResults;
            Renaming = renaming;
            UpdatePackages = updatePackages;
        }
    }

    #endregion
}