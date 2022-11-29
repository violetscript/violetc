namespace VioletScript.Parser.Semantic.Logic;

using System.Collections.Generic;
using VioletScript.Parser.Operator;
using VioletScript.Parser.Semantic.Model;

/// <summary>
/// Logic for overriding methods.
/// It is not allowed to override a parameterized method, as a <c>CannotOverrideGenericMethodIssue</c>
/// is produced.
/// </summary>