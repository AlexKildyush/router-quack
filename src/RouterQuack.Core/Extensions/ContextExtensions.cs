namespace RouterQuack.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Context"/>.
/// </summary>
public static class ContextExtensions
{
    /// <param name="context">The execution context to update.</param>
    extension(Context context)
    {
        /// <summary>
        /// Mark the current execution context as failed.
        /// </summary>
        public void ApplyError()
        {
            context.ErrorsOccurred = true;
        }

        /// <summary>
        /// Mark the current execution context as warning.
        /// </summary>
        public void ApplyWarning()
        {
            if (context.Strict)
                context.ErrorsOccurred = true;
        }
    }
}