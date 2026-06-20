using System;
using System.Collections.Generic;
using System.Linq;

namespace Screentation;

public class HistoryManager
{
    private readonly ScreenshotSession _session;
    private readonly List<List<AnnotationElement>> _undoStack = new();
    private readonly List<List<AnnotationElement>> _redoStack = new();
    private const int MaxHistory = 50;

    // Flag to avoid saving states during undo/redo operations
    private bool _isApplyingHistory = false;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? HistoryChanged;

    public HistoryManager(ScreenshotSession session)
    {
        _session = session;
    }

    public void SaveState()
    {
        if (_isApplyingHistory) return;

        // Take a deep copy of the current annotations list
        var state = _session.Annotations.Select(e => e.Clone()).ToList();
        _undoStack.Add(state);

        if (_undoStack.Count > MaxHistory)
        {
            _undoStack.RemoveAt(0);
        }

        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo) return;

        _isApplyingHistory = true;
        try
        {
            // Push current state to redo stack
            var currentState = _session.Annotations.Select(e => e.Clone()).ToList();
            _redoStack.Add(currentState);

            // Pop state from undo stack
            var previousState = _undoStack[^1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            // Restore annotations
            _session.Annotations.Clear();
            foreach (var element in previousState)
            {
                _session.Annotations.Add(element);
            }
        }
        finally
        {
            _isApplyingHistory = false;
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;

        _isApplyingHistory = true;
        try
        {
            // Push current state to undo stack
            var currentState = _session.Annotations.Select(e => e.Clone()).ToList();
            _undoStack.Add(currentState);

            // Pop state from redo stack
            var nextState = _redoStack[^1];
            _redoStack.RemoveAt(_redoStack.Count - 1);

            // Restore annotations
            _session.Annotations.Clear();
            foreach (var element in nextState)
            {
                _session.Annotations.Add(element);
            }
        }
        finally
        {
            _isApplyingHistory = false;
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveLastStateIfIdentical()
    {
        if (_undoStack.Count == 0) return;
        var lastState = _undoStack[^1];
        if (AreStatesEqual(lastState, _session.Annotations))
        {
            _undoStack.RemoveAt(_undoStack.Count - 1);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool AreStatesEqual(List<AnnotationElement> state1, IList<AnnotationElement> state2)
    {
        if (state1.Count != state2.Count) return false;
        for (int i = 0; i < state1.Count; i++)
        {
            var e1 = state1[i];
            var e2 = state2[i];
            if (e1.Id != e2.Id) return false;
            if (e1.Type != e2.Type) return false;
            if (e1.Color != e2.Color) return false;
            if (e1.StrokeThickness != e2.StrokeThickness) return false;

            switch (e1)
            {
                case RectElement r1 when e2 is RectElement r2:
                    if (r1.Bounds != r2.Bounds || r1.HasFill != r2.HasFill) return false;
                    break;
                case StepElement s1 when e2 is StepElement s2:
                    if (s1.Center != s2.Center || s1.Radius != s2.Radius || s1.Number != s2.Number) return false;
                    break;
                case ArrowElement a1 when e2 is ArrowElement a2:
                    if (a1.Start != a2.Start || a1.End != a2.End) return false;
                    break;
                case BlurElement b1 when e2 is BlurElement b2:
                    if (b1.Bounds != b2.Bounds || b1.BlurRadius != b2.BlurRadius) return false;
                    break;
                case EraserElement er1 when e2 is EraserElement er2:
                    if (er1.Bounds != er2.Bounds || er1.SourceOffset != er2.SourceOffset) return false;
                    break;
            }
        }
        return true;
    }
}
