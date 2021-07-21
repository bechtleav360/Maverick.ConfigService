using Prometheus;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     definitions of all available Metrics for this Service
    /// </summary>
    internal static class KnownMetrics
    {
        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationErrors = Metrics.CreateCounter(
            "compilation_errors",
            "Configuration-Compilation Errors",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationFallbackValuesResolved = Metrics.CreateCounter(
            "compilation_resolved_fallbacks",
            "Fallback-Values used during Compilation",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationIndirectionsResolved = Metrics.CreateCounter(
            "compilation_resolved_indirection",
            "Indirect References resolved during Compilation",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationRangeReferenceResolved = Metrics.CreateCounter(
            "compilation_resolved_range_reference",
            "Range References resolved during Compilation",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationScalarReferenceResolved = Metrics.CreateCounter(
            "compilation_resolved_scalar_reference",
            "Scalar References resolved during Compilation",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationValuesResolved = Metrics.CreateCounter(
            "compilation_resolved_static",
            "Static Values resolved during Compilation",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter CompilationWarnings = Metrics.CreateCounter(
            "compilation_warnings",
            "Configuration-Compilation Warnings",
            new CounterConfiguration());

        /// <summary>
        ///     counts how many warnings were reported during the compilation of a Configuration
        /// </summary>
        internal static readonly Counter ConfigurationsCompiled = Metrics.CreateCounter(
            "compilation_executed",
            "Configurations Compiled",
            new CounterConfiguration());

        /// <summary>
        ///     counts how often Connection-Infos have been retrieved
        /// </summary>
        internal static readonly Counter ConnectionInfo = Metrics.CreateCounter(
            "connection_infos_retrieved",
            "How often Connection-Infos have been retrieved",
            new CounterConfiguration());

        /// <summary>
        ///     counts how often Configurations have been converted from one to another representation
        /// </summary>
        internal static readonly Counter Conversion = Metrics.CreateCounter(
            "convert_configuration",
            "Configurations converted from one to another representation",
            new CounterConfiguration {LabelNames = new[] {"direction"}});

        /// <summary>
        ///     counts how many DomainEvents have been projected to the local database
        /// </summary>
        internal static readonly Counter EventsProjected = Metrics.CreateCounter(
            "domain_events_projected",
            "DomainEvents Projected",
            new CounterConfiguration {LabelNames = new[] {"event_type"}});

        /// <summary>
        ///     counts how many DomainEvents have been checked for validity
        /// </summary>
        internal static readonly Counter EventsValidated = Metrics.CreateCounter(
            "domain_events_validated",
            "DomainEvents Valildated",
            new CounterConfiguration {LabelNames = new[] {"validity"}});

        /// <summary>
        ///     counts how many DomainEvents have been written to the EventStore
        /// </summary>
        internal static readonly Counter EventsWritten = Metrics.CreateCounter(
            "domain_events_written",
            "DomainEvents Written",
            new CounterConfiguration {LabelNames = new[] {"event_type"}});

        /// <summary>
        ///     counts how many internal Exceptions have been caught without bubbling up to the User
        /// </summary>
        internal static readonly Counter Exception = Metrics.CreateCounter(
            "internal_exceptions",
            "Internal Exceptions",
            new CounterConfiguration {LabelNames = new[] {"exception_type"}});

        /// <summary>
        ///     counts how many DomainEvents have been projected to the local database
        /// </summary>
        internal static readonly Histogram ProjectionTime = Metrics.CreateHistogram(
            "domain_events_projected_duration",
            "DomainEvents Projected",
            new HistogramConfiguration {LabelNames = new[] {"event_type"}});

        /// <summary>
        ///     counts how many Temporary Keys have been set
        /// </summary>
        internal static readonly Counter TemporaryKeyCreated = Metrics.CreateCounter(
            "temporary_keys_set",
            "Temporary-Key Set",
            new CounterConfiguration());
    }
}
