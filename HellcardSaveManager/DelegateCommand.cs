using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace HellcardSaveManager
{
	/// <inheritdoc />
	/// <summary>
	///     This class allows delegating the commanding logic to methods passed as parameters,
	///     and enables a View to bind commands to objects that are not part of the element tree.
	/// </summary>
	public class DelegateCommand : ICommand
	{
		// ReSharper disable UnusedMember.Global
		public Key GestureKey { get; set; }
		public ModifierKeys GestureModifier { get; set; }
		public MouseAction MouseGesture { get; set; }
		// ReSharper restore UnusedMember.Global

		public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod = null, bool isAutomaticRequeryDisabled = false)
		{
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
			_canExecuteMethod = canExecuteMethod;
			_isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
		}

		/// <summary>
		///     Method to determine if the command can be executed
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public bool CanExecute()
		{
			// ReSharper disable once MergeSequentialChecksWhenPossible
			return _canExecuteMethod == null || _canExecuteMethod();
		}

		/// <summary>
		///     Execution of the command
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public void Execute()
		{
			_executeMethod?.Invoke();
		}

		/// <summary>
		///     Property to enable or disable CommandManager's automatic requery on this command
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public bool IsAutomaticRequeryDisabled
		{
			get => _isAutomaticRequeryDisabled;
            set
			{
				if (_isAutomaticRequeryDisabled == value)
					return;

				if (value)
				{
					CommandManagerHelper.RemoveHandlersFromRequerySuggested(_canExecuteChangedHandlers);
				}
				else
				{
					CommandManagerHelper.AddHandlersToRequerySuggested(_canExecuteChangedHandlers);
				}
				_isAutomaticRequeryDisabled = value;
			}
		}

		/// <summary>
		///     Raises the CanExecuteChanged event
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public void RaiseCanExecuteChanged()
		{
			OnCanExecuteChanged();
		}

		/// <summary>
		///     Protected virtual method to raise CanExecuteChanged event
		/// </summary>
		private void OnCanExecuteChanged()
		{
			CommandManagerHelper.CallWeakReferenceHandlers(_canExecuteChangedHandlers);
		}

		#region ICommand Members

		/// <inheritdoc />
		/// <summary>
		///     ICommand.CanExecuteChanged implementation
		/// </summary>
		public event EventHandler CanExecuteChanged
		{
			add
			{
				if (!_isAutomaticRequeryDisabled)
				{
					CommandManager.RequerySuggested += value;
				}
				CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
			}
			remove
			{
				if (!_isAutomaticRequeryDisabled)
				{
					CommandManager.RequerySuggested -= value;
				}
				CommandManagerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
			}
		}

		bool ICommand.CanExecute(object parameter)
		{
			return CanExecute();
		}

		void ICommand.Execute(object parameter)
		{
			Execute();
		}

		#endregion

		private readonly Action _executeMethod;
		private readonly Func<bool> _canExecuteMethod;
		private bool _isAutomaticRequeryDisabled;
		private List<WeakReference> _canExecuteChangedHandlers;
	}

	/// <inheritdoc />
	/// <summary>
	///     This class allows delegating the commanding logic to methods passed as parameters,
	///     and enables a View to bind commands to objects that are not part of the element tree.
	/// </summary>
	/// <typeparam name="T">Type of the parameter passed to the delegates</typeparam>
    // ReSharper disable once UnusedMember.Global
    public class DelegateCommand<T> : ICommand
	{
		public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod = null, bool isAutomaticRequeryDisabled = false)
		{
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
			_canExecuteMethod = canExecuteMethod;
			_isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
		}

		/// <summary>
		///     Method to determine if the command can be executed
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public bool CanExecute(T parameter)
		{
			// ReSharper disable once MergeSequentialChecksWhenPossible
			return _canExecuteMethod == null || _canExecuteMethod(parameter);
		}

		/// <summary>
		///     Execution of the command
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public void Execute(T parameter)
		{
			_executeMethod?.Invoke(parameter);
		}

		/// <summary>
		///     Raises the CanExecuteChanged event
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public void RaiseCanExecuteChanged()
		{
			OnCanExecuteChanged();
		}

		/// <summary>
		///     Protected virtual method to raise CanExecuteChanged event
		/// </summary>
		private void OnCanExecuteChanged()
		{
			CommandManagerHelper.CallWeakReferenceHandlers(_canExecuteChangedHandlers);
		}

		/// <summary>
		///     Property to enable or disable CommandManager's automatic requery on this command
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public bool IsAutomaticRequeryDisabled
		{
			get => _isAutomaticRequeryDisabled;
            set
			{
				if (_isAutomaticRequeryDisabled == value)
					return;

				if (value)
				{
					CommandManagerHelper.RemoveHandlersFromRequerySuggested(_canExecuteChangedHandlers);
				}
				else
				{
					CommandManagerHelper.AddHandlersToRequerySuggested(_canExecuteChangedHandlers);
				}
				_isAutomaticRequeryDisabled = value;
			}
		}

		/// <inheritdoc />
		/// <summary>
		///     ICommand.CanExecuteChanged implementation
		/// </summary>
		public event EventHandler CanExecuteChanged
		{
			add
			{
				if (!_isAutomaticRequeryDisabled)
				{
					CommandManager.RequerySuggested += value;
				}
				CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
			}
			remove
			{
				if (!_isAutomaticRequeryDisabled)
				{
					CommandManager.RequerySuggested -= value;
				}
				CommandManagerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
			}
		}

		bool ICommand.CanExecute(object parameter)
		{
			// if T is of value type and the parameter is not
			// set yet, then return false if CanExecute delegate
			// exists, else return true
			if (parameter == null &&
				typeof(T).IsValueType)
			{
				return _canExecuteMethod == null;
			}

			return parameter is T variable && CanExecute(variable);
		}

		void ICommand.Execute(object parameter)
		{
			Execute((T)parameter);
		}

		private readonly Action<T> _executeMethod;
		private readonly Func<T, bool> _canExecuteMethod;
		private bool _isAutomaticRequeryDisabled;
		private List<WeakReference> _canExecuteChangedHandlers;
	}

	/// <summary>
	///     This class contains methods for the CommandManager that help avoid memory leaks by
	///     using weak references.
	/// </summary>
	internal static class CommandManagerHelper
	{
		internal static void CallWeakReferenceHandlers(List<WeakReference> handlers)
		{
			if (handlers == null)
				return;

			// Take a snapshot of the handlers before we call out to them since the handlers
			// could cause the array to me modified while we are reading it.

			var callees = new EventHandler[handlers.Count];
			var count = 0;

			for (var i = handlers.Count - 1; i >= 0; i--)
			{
				var reference = handlers[i];
                if (!(reference.Target is EventHandler handler))
				{
					// Clean up old handlers that have been collected
					handlers.RemoveAt(i);
				}
				else
				{
					callees[count] = handler;
					count++;
				}
			}

			// Call the handlers that we snapshotted
			for (var i = 0; i < count; i++)
			{
				var handler = callees[i];
				handler(null, EventArgs.Empty);
			}
		}

		internal static void AddHandlersToRequerySuggested(List<WeakReference> handlers)
		{
			if (handlers == null)
				return;

			foreach (var handler in handlers.Select(handlerRef => handlerRef.Target).OfType<EventHandler>())
			{
				CommandManager.RequerySuggested += handler;
			}
		}

		internal static void RemoveHandlersFromRequerySuggested(List<WeakReference> handlers)
		{
			if (handlers == null)
				return;

			foreach (var handler in handlers.Select(handlerRef => handlerRef.Target).OfType<EventHandler>())
			{
				CommandManager.RequerySuggested -= handler;
			}
		}

		// ReSharper disable once UnusedMember.Global
		internal static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler)
		{
			AddWeakReferenceHandler(ref handlers, handler, -1);
		}

		internal static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler, int defaultListSize)
		{
			if (handlers == null)
			{
				handlers = defaultListSize > 0 ? new List<WeakReference>(defaultListSize) : new List<WeakReference>();
			}

			handlers.Add(new WeakReference(handler));
		}

		internal static void RemoveWeakReferenceHandler(List<WeakReference> handlers, EventHandler handler)
		{
			if (handlers == null)
				return;

			for (var i = handlers.Count - 1; i >= 0; i--)
			{
				var reference = handlers[i];
                if (!(reference.Target is EventHandler existingHandler) || (existingHandler == handler))
				{
					// Clean up old handlers that have been collected
					// in addition to the handler that is to be removed.
					handlers.RemoveAt(i);
				}
			}
		}
	}

}
