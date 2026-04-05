// Copyright (c) 2026 Danylo Fitel

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Priority queue interface.", Scope = "type", Target = "~T:LibSharp.Collections.IPriorityQueue`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Max priority queue implementation.", Scope = "type", Target = "~T:LibSharp.Collections.MaxPriorityQueue`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Min priority queue implementation.", Scope = "type", Target = "~T:LibSharp.Collections.MinPriorityQueue`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Priority queue naming is intentional and part of the public API.", Scope = "type", Target = "~T:LibSharp.Collections.IPriorityQueue`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Max priority queue naming is intentional and part of the public API.", Scope = "type", Target = "~T:LibSharp.Collections.MaxPriorityQueue`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Min priority queue naming is intentional and part of the public API.", Scope = "type", Target = "~T:LibSharp.Collections.MinPriorityQueue`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Optional<T> is the established public API name for the option-like container.", Scope = "type", Target = "~T:LibSharp.Common.Optional`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Ok/Fail factory methods are the intended construction API for Result<T, TError>.", Scope = "type", Target = "~T:LibSharp.Common.Result`2")]
