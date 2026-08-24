using System.Runtime.CompilerServices;

// Rank Advisor moved into Optimizer. Doctors still consumes the same internal
// diagnostics/rank model, so expose it explicitly instead of duplicating those
// types across packages.
[assembly: InternalsVisibleTo("NekoSune.Doctors.Editor")]
