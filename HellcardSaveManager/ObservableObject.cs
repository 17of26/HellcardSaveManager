using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HellcardSaveManager
{
    /// <inheritdoc cref="INotifyPropertyChanged" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// This is the abstract base class for any object that provides property change notifications.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged, IDisposable
	{
		static ObservableObject()
		{
			_eventArgCache = new Dictionary<string, PropertyChangedEventArgs>();
		}

		protected ObservableObject()
		{

		}

		/// <summary>
		/// Provides the option to capture the current synchronization context on construction
		/// and use that to raise property change events.
		/// </summary>
		/// <param name="useSynchronizationContext"></param>
		protected ObservableObject(bool useSynchronizationContext)
		{
			if (useSynchronizationContext)
				SynchronizationContext = SynchronizationContext.Current;
		}

		~ObservableObject()
		{
			if (!IsDisposed)
			{
				VerifyDisposal();
				Dispose(false);
			}

			IsDisposed = true;
		}

		#region INotifyPropertyChanged Implementation

		/// <inheritdoc />
		/// <summary>
		/// Raised when a property on this object has a new value.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Derived classes can override this method to execute logic after a property is set. The
		/// base implementation does nothing.
		/// </summary>
		/// <param name="propertyName">The property which was changed.</param>
		protected virtual void AfterPropertyChanged(string propertyName)
		{
		}

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">Name of property that has a new value.  Use an empty string to indicate
		/// that all properties changed</param>
		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			VerifyPropertyName(propertyName);

			var handler = PropertyChanged;
			if (handler != null)
			{
				// Get the cached event args.
				var args = string.IsNullOrEmpty(propertyName) ? new PropertyChangedEventArgs("") :
																GetPropertyChangedEventArgs(propertyName);

				if (SynchronizationContext != null && SynchronizationContext != SynchronizationContext.Current)
					SynchronizationContext.Send(state => handler(this, args), null);
				else
					handler(this, args);
			}

			AfterPropertyChanged(propertyName);
		}

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="args">PropertyChangedEventArgs for the property being changed</param>
		protected void RaisePropertyChanged(PropertyChangedEventArgs args)
		{
			VerifyPropertyName(args.PropertyName);

			if (SynchronizationContext != null && SynchronizationContext != SynchronizationContext.Current)
				SynchronizationContext.Send(state => PropertyChanged?.Invoke(this, args), null);
			else
				PropertyChanged?.Invoke(this, args);

			AfterPropertyChanged(args.PropertyName);
		}

		#endregion

		#region Helper methods

		/// <summary>
		/// Compares currentValue to newValue and if they are different, assigns newValue to currentValue and
		/// fires PropertyChanged with specified eventArgs
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="currentValue"></param>
		/// <param name="newValue"></param>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		protected bool SetProperty<T>(ref T currentValue, ref T newValue, [CallerMemberName] string propertyName = null )
		{
			if (Equals(currentValue, newValue))
				return false;

			currentValue = newValue;
			// ReSharper disable once ExplicitCallerInfoArgument
			RaisePropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Returns an instance of PropertyChangedEventArgs for
		/// the specified property name.
		/// </summary>
		/// <param name="propertyName">
		/// The name of the property to create event args for.
		/// </param>
		// ReSharper disable once MemberCanBePrivate.Global
		public static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
				throw new ArgumentException("propertyName cannot be null or empty.");

			PropertyChangedEventArgs args;

			// Get the event args from the cache, creating them and adding to the cache if necessary.
			lock (typeof(ObservableObject))
			{
				var isCached = _eventArgCache.ContainsKey(propertyName);
				if (!isCached)
				{
					_eventArgCache.Add(propertyName, new PropertyChangedEventArgs(propertyName));
				}

				args = _eventArgCache[propertyName];
			}

			return args;
		}
		#endregion

		#region IDisposable Implementation

		/// <inheritdoc />
		/// <summary>
		/// Invoked when this object is being removed from the application and will be subject to garbage collection
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			IsDisposed = true;
		}

		/// <summary>
		/// Dispose hook for child classes to perform cleanup
		/// </summary>
		/// <param name="disposing">Set to true if calling from Dispose, false if calling from a finalizer</param>
		protected virtual void Dispose(bool disposing)
		{
		}

		private bool IsDisposed { get; set; }

		#endregion

		#region Debugging Aides
		/// <summary>
		/// Warns the developer if this object does not have a public property with the specified name. This
		/// method does not exist in a Release build.
		/// </summary>
		[Conditional("DEBUG")]
		private void VerifyPropertyName(string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
				return;

			var type = GetType();

			// Look for a public property with the specified name.
			var propInfo = type.GetProperty(propertyName);

			if (propInfo == null)
			{
				Debug.Fail("{propertyName} is not a public property of {type.FullName}");
			}
		}

		/// <summary>
		/// Warns the developer if an object needs disposal but was not disposed.
		/// This method does not exist in Release build
		/// </summary>
		[Conditional("DEBUG")]
		private void VerifyDisposal()
		{
			if (MustBeDisposed && !IsDisposed)
			{
				Debug.Fail($"Object {GetType()} requires disposal and was not disposed");
			}
		}

		protected virtual bool MustBeDisposed => false;

		#endregion

		// Cache of PropertyChangedEventArgs instances, so that only one instance will exist for each property name
		// passed through this class.  This caching can dramatically reduce the managed heap fragmentation caused
		// by a property being set many times in a short period of time.
		// The reason GetPropertyChangedEventArgs public is so that any class can take advantage of its caching mechanism,
		// even if the class does not descend from ObservableObject.
		private static readonly Dictionary<string, PropertyChangedEventArgs> _eventArgCache;

	    protected SynchronizationContext SynchronizationContext { get; }
	}
}

