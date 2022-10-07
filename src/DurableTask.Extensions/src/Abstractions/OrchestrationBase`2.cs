// Copyright (c) Jacob Viau. All rights reserved.
// Licensed under the APACHE 2.0. See LICENSE file in the project root for full license information.

using DurableTask.Core;
using DurableTask.Core.Serializing;
using DurableTask.Extensions.Converters;
using DurableTask.Extensions.Logging;
using DurableTask.Extensions.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DurableTask.Extensions.Abstractions;

/// <summary>
/// A base <see cref="TaskOrchestration" /> with additional semantics..
/// </summary>
/// <typeparam name="TInput">The input for the orchestration.</typeparam>
/// <typeparam name="TResult">The result of the orchestration.</typeparam>
public abstract class OrchestrationBase<TInput, TResult> : TaskOrchestration<TResult, TInput>, IOrchestrationBase
    where TInput : IOrchestrationRequest<TResult>
{
    private OrchestrationContext? _context;

    /// <inheritdoc />
    /// <remarks>
    /// This will be set by middleware.
    /// </remarks>
    public string Name { get; private set; } = string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// This will be set by middleware.
    /// </remarks>
    public string? Version { get; private set; }

    /// <inheritdoc />
    /// <remarks>
    /// This will be set by middleware.
    /// </remarks>
    public ILogger Logger { get; private set; } = NullLogger.Instance;

    /// <summary>
    /// Gets the orchestration context.
    /// </summary>
    protected OrchestrationContext Context => _context!;

    /// <inheritdoc />
    public override Task<TResult> RunTask(OrchestrationContext context, TInput input)
    {
        Check.NotNull(context, nameof(context));
        _context = PrepareContext(context);

        // Wrap the logger the middleware gave us with an orchestration specific logger.
        // This logger will not log when the OrchestrationContext is replaying.
        if (Logger is not null)
        {
            Logger = Logger switch
            {
                OrchestrationLogger ol => new OrchestrationLogger(context, ol.InnerLogger),
                _ => new OrchestrationLogger(context, Logger),
            };
        }

        return RunAsync(input);
    }

    /// <summary>
    /// Executes this orchestration instance.
    /// </summary>
    /// <param name="input">The input for this orchestration.</param>
    /// <returns>The result of this orchestration.</returns>
    protected abstract Task<TResult> RunAsync(TInput input);

    private OrchestrationContext PrepareContext(OrchestrationContext context)
    {
        if (DataConverter is null)
        {
            return context;
        }

        if (DataConverter is not JsonDataConverter jsonDataConverter)
        {
            jsonDataConverter = new JsonDataConverterShim(DataConverter);
        }

        context.MessageDataConverter = jsonDataConverter;
        context.ErrorDataConverter = jsonDataConverter;
        return context;
    }

    void IOrchestrationBase.Initialize(string name, string? version, ILogger logger, DataConverter converter)
    {
        Name = Check.NotNull(name);
        Version = version;
        Logger = Check.NotNull(logger);
        DataConverter = Check.NotNull(converter);
    }
}