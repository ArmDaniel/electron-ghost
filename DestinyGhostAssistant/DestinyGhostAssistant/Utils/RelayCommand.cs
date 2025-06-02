using System;
using System.Windows.Input;

namespace DestinyGhostAssistant.Utils
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        /// <summary>
        /// Manually raises the CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(ConvertParameter(parameter));
        }

        public void Execute(object? parameter)
        {
            _execute(ConvertParameter(parameter));
        }

        private T? ConvertParameter(object? parameter)
        {
            if (parameter == null)
            {
                // If T is a value type and not nullable, this will result in default(T), e.g., 0 for int.
                // If T is a nullable value type (e.g., int?), this will be null.
                // If T is a reference type, this will be null.
                return default;
            }

            if (parameter is T typedParam)
            {
                return typedParam;
            }

            try
            {
                // This might throw if conversion is not possible, e.g., trying to convert a complex object to string implicitly.
                // Or converting string "abc" to int.
                return (T?)Convert.ChangeType(parameter, typeof(T));
            }
            catch (InvalidCastException) // Or FormatException, OverflowException from ChangeType
            {
                // For debugging: Log this error. For production, decide if this should throw or return default.
                System.Diagnostics.Debug.WriteLine($"RelayCommand<T>: Parameter type mismatch. Expected {typeof(T)}, got {parameter.GetType()}. Value: {parameter}");
                return default; // Or rethrow, depending on desired strictness.
            }
        }

        /// <summary>
        /// Manually raises the CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
