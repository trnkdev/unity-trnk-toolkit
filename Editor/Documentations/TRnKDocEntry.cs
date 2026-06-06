#if UNITY_EDITOR
using System;
using UnityEngine;

namespace TRnK.Toolkit
{
    internal enum DocCategory { Core, Components, Extensions, Utilities, EditorTools, TRnKSignal, TRnKFlow, TRnKSerializer, TRnKTimer }
    internal enum DocMemberKind { Property, Method, Callback }

    [Serializable]
    internal sealed class DocMember
    {
        public DocMemberKind Kind;
        /// <summary>Short signature shown in the Name column, e.g. "RemainingTime" or "Start()" or "OnComplete(Action)".</summary>
        public string Signature;
        /// <summary>One-line description shown in the Description column.</summary>
        public string Summary;
        /// <summary>Production-ready code example revealed when the row is expanded.</summary>
        public string Code;
    }

    [Serializable]
    internal sealed class TRnKDocEntry
    {
        public string Title;
        public string Namespace;
        [TextArea(2, 4)] public string Summary;
        [TextArea(3, 12)] public string Description;
        /// <summary>Optional overview code example shown at the bottom of the expanded card.</summary>
        [TextArea(4, 30)] public string Code;
        public string[] Tags;
        public DocCategory Category;
        /// <summary>Structured API members rendered as Property / Method / Callback tables.</summary>
        public DocMember[] Members;
    }
}
#endif
