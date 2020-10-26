﻿
// FarNet.Tools library for FarNet
// Copyright (c) Roman Kuzmin

using System;
using System.Threading;
using FarNet.Forms;

namespace FarNet.Tools
{
	/// <summary>
	/// Andvanced form to show progress of potentially long background jobs.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Consider to use much simpler <see cref="ProgressBox"/>.
	/// This form is useful in cases that allow job thread abortion.
	/// </para>
	/// <para>
	/// This form should be created and shown in the main thread.
	/// Some members are designed for use in other threads, for example:
	/// normal cases: <see cref="Activity"/>, <see cref="SetProgressValue"/>, <see cref="Complete"/>;
	/// cancellation cases: <see cref="Close"/>, <see cref="IsClosed"/>, <see cref="Canceled"/>.
	/// </para>
	/// <para>
	/// The form can be shown once and cannot be reused after closing.
	/// </para>
	/// <para>
	/// The standard scenario:
	/// <ul>
	/// <li>create a progress form but do not show yet;</li>
	/// <li>start a job in another thread and give it this form;</li>
	/// <li>let the main thread to sleep a bit: a fast job may complete;</li>
	/// <li>show the form; the progress form is shown if a job is not yet done.</li>
	/// </ul>
	/// </para>
	/// There is yet another simpler scenario using the <see cref="Invoke"/>, see remarks there.
	/// </remarks>
	public sealed class ProgressForm : Form, IProgress
	{
		readonly object _lock = new object();
		int _LineCount = 1;
		bool _isCompleted;
		bool _isCanceled;
		bool _isClosed;

		readonly Progress _progress = new Progress();
		readonly Thread _mainThread;

		Thread _jobThread;
		Exception _jobError;

		IText[] _textActivity;
		IText _textProgress;

		/// <summary>
		/// New progress form.
		/// </summary>
		/// <remarks>
		/// It should be created and then shown in the main thread.
		/// </remarks>
		public ProgressForm()
		{
			_mainThread = Thread.CurrentThread;
		}

		/// <summary>
		/// Gets or sets text line count.
		/// </summary>
		/// <remarks>
		/// It should be set before the show.
		/// The default is 1.
		/// </remarks>
		public int LineCount
		{
			get { return _LineCount; }
			set
			{
				if (value < 1 || value > Progress.TEXT_HEIGHT) throw new ArgumentOutOfRangeException("value");
				_LineCount = value;
			}
		}

		/// <summary>
		/// Tells to show the <b>Cancel</b> button.
		/// </summary>
		/// <remarks>
		/// False: a user cannot cancel the progress form and jobs in progress.
		/// The form is opened until <see cref="Complete"/> or <see cref="Close"/> is called.
		/// <para>
		/// True: a user can cancel the progress form.
		/// A job has to support this: it should check the <see cref="IsClosed"/> periodically
		/// or listen to the <see cref="Canceled"/> event; if any of these happens the job
		/// has to exit as soon as possible.
		/// </para>
		/// </remarks>
		public bool CanCancel { get; set; }

		/// <summary>
		/// Called when the form is about to be canceled. It can abort canceling by <see cref="ClosingEventArgs.Ignore"/>.
		/// </summary>
		public event EventHandler<ClosingEventArgs> Canceling;

		/// <summary>
		/// Called when the form is canceled by a user or closed by the <see cref="Close"/>.
		/// </summary>
		public event EventHandler Canceled;

		/// <summary>
		/// Called periodically from the main thread.
		/// </summary>
		/// <remarks>
		/// This event may be used in advanced scenarios with job interruption and resuming
		/// and user interaction with dialogs. A handler is called from the main thread and
		/// may use the API as usual.
		/// </remarks>
		public event EventHandler Idled;

		/// <summary>
		/// Gets true if a closing method has been called or a user has canceled the form.
		/// </summary>
		/// <remarks>
		/// Jobs may check this property periodically and exit as soon as it is true.
		/// Alternatively, they may listen to the <see cref="Canceled"/> event.
		/// </remarks>
		public bool IsClosed
		{
			get { return _isClosed || _isCanceled; }
		}

		/// <summary>
		/// Gets true if the <see cref="Complete"/> has been called.
		/// </summary>
		/// <remarks>
		/// If it is true then <see cref="IsClosed"/> is also true.
		/// </remarks>
		public bool IsCompleted
		{
			get { return _isCompleted; }
		}

		/// <summary>
		/// Closes the form and triggers the <see cref="Canceled"/> event.
		/// </summary>
		/// <remarks>
		/// This method is thread safe and can be called from jobs in order to cancel.
		/// But normally jobs should call <see cref="Complete"/> when they are done.
		/// <para>
		/// The method <see cref="Show"/> returns false if the form is closed by this method.
		/// </para>
		/// <para>
		/// Note that closing by this method does not trigger canceling events.
		/// </para>
		/// </remarks>
		public override void Close()
		{
			lock (_lock)
			{
				if (_isClosed)
					return;

				_isClosed = true;

				if (_jobThread != null)
					_jobThread.Abort();

				//! mind another thread
				if (Thread.CurrentThread == _mainThread)
				{
					base.Close();
				}
				else
				{
					Far.Api.PostJob(delegate { base.Close(); });
				}
			}
		}

		/// <summary>
		/// Closes the form when the job is complete.
		/// </summary>
		/// <remarks>
		/// This method is thread safe and designed for jobs.
		/// Normally when a job is done it calls this method.
		/// The <see cref="Show"/> returns true if the form is closed by this method.
		/// </remarks>
		public void Complete()
		{
			// lock it: Close() can be in progress right now; wait and do nothing then
			lock (_lock)
			{
				if (_isClosed)
					return;

				_isCompleted = true;
				Close();
			}
		}

		/// <summary>
		/// Shows the progress form or returns the result if the job is already done.
		/// </summary>
		/// <returns>True if the <see cref="Complete"/> has been called and false in all other cases.</returns>
		/// <remarks>
		/// This method should be called in the main thread after starting a job in another thread.
		/// Normally it shows the modal dialog and blocks the main thread.
		/// <para>
		/// The form is closed when a job calls the <see cref="Complete"/> or <see cref="Close"/> or
		/// a user cancels the form when <see cref="CanCancel"/> is true.
		/// </para>
		/// <para>
		/// If a job is fast and has already closed the form this methods returns immediately without showing a dialog.
		/// </para>
		/// </remarks>
		public override bool Show()
		{
			if (_isClosed)
				return _isCompleted && !_isCanceled;

			Init();

			try
			{
				base.Show();
				return _isCompleted && !_isCanceled;
			}
			finally
			{
				Far.Api.UI.SetProgressState(TaskbarProgressBarState.NoProgress);
				Far.Api.UI.SetProgressFlash();
			}
		}

		void OnInitialized(object sender, InitializedEventArgs e)
		{
			// do not show the form if it is already closed
			if (_isClosed)
				e.Ignore = true;
		}

		bool AbortCanceling()
		{
			if (Canceling == null)
				return false;

			var args = new ClosingEventArgs(null);
			Canceling(this, args);
			return args.Ignore;
		}

		void OnClosing(object sender, ClosingEventArgs e)
		{
			lock (_lock)
			{
				// allow the dialog to close if the form is closed
				if (_isClosed)
					return;

				// do not close if cannot or aborted
				if (!CanCancel || AbortCanceling())
				{
					e.Ignore = true;
					return;
				}

				// flag
				_isCanceled = true;

				// abort
				if (_jobThread != null)
					Pfz.Threading.SafeAbort.Abort(_jobThread, 4000, 2000, 1000, true);

				// notify
				Canceled?.Invoke(this, null);
			}
		}

		void OnIdled(object sender, EventArgs e)
		{
			// if the form is closed and the dialog is still alive the closed the dialog directly
			if (_isClosed)
			{
				Dialog.Close();
				return;
			}

			// event
			Idled?.Invoke(this, null);

			// show
			var lines = _progress.Build(out string progress, _textActivity.Length);
			for (int iLine = 0; iLine < _LineCount && iLine < lines.Length; ++iLine)
				_textActivity[iLine].Text = lines[iLine];
			_textProgress.Text = progress;
		}

		void Init()
		{
			Dialog.KeepWindowTitle = true;

			SetSize(Progress.FORM_WIDTH, (CanCancel ? 7 : 5) + _LineCount);

			_textActivity = new IText[_LineCount];
			for (int iLine = 0; iLine < _LineCount; ++iLine)
				_textActivity[iLine] = Dialog.AddText(5, -1, 5 + Progress.TEXT_WIDTH - 1, string.Empty);
			_textProgress = Dialog.AddText(5, -1, 5 + Progress.TEXT_WIDTH - 1, string.Empty);

			if (CanCancel)
			{
				Dialog.AddText(5, -1, 0, string.Empty).Separator = 1;

				IButton button = Dialog.AddButton(0, -1, _jobThread == null ? "Cancel" : "Abort");
				button.CenterGroup = true;
				Dialog.Default = button;
			}

			Dialog.Initialized += OnInitialized;
			Dialog.Closing += OnClosing;
			Dialog.Idled += OnIdled;
		}

		/// <summary>
		/// Invokes the job in a new thread (simplified scenario with optional job thread abortion by a user).
		/// </summary>
		/// <param name="job">The job action delegate to be invoked in a new thread. It should either complete or throw any exception.</param>
		/// <returns>Null if the job has completed or an exception thrown by the job or the <see cref="OperationCanceledException"/>.</returns>
		/// <remarks>
		/// This way is much simpler than the standard 4-steps scenario and it is recommended for not abortable jobs.
		/// <para>
		/// If the <see cref="CanCancel"/> is true then on user cancellation the job thread is aborted.
		/// It many cases this seems to be fine but the job has to be carefully designed for that.
		/// In particular the <see cref="ThreadAbortException"/> can be thrown at any moment.
		/// If there are potential unwanted effects of job abortion then do not use this way.
		/// </para>
		/// <para>
		/// This way is not suitable for PowerShell scripts in any case.
		/// Scripts should use the standard 4-steps scenario with standard PowerShell or simple PowerShellFar background jobs.
		/// </para>
		/// </remarks>
		public Exception Invoke(ThreadStart job)
		{
			// share the new thread and start it
			_jobThread = new Thread(() => Job(job));
			_jobThread.Start();

			// wait a little bit
			Thread.Sleep(500);

			// show the form and return null if it is completed
			if (Show())
				return null;

			// get the error
			return _jobError ?? new OperationCanceledException();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void Job(ThreadStart job)
		{
			try
			{
				// do the job in this thread
				job();

				// done, complete and return
				Complete();
				return;
			}
			catch (ThreadAbortException)
			{
				// convert to canceled
				_jobError = new OperationCanceledException();
			}
			catch (Exception ex)
			{
				// to be returned by Invoke()
				_jobError = ex;
			}

			// close on errors
			Close();
		}

		#region IProgress

		/// <summary>
		/// Gets or sets the current activity description.
		/// </summary>
		public string Activity
		{
			get { return _progress.Activity; }
			set { _progress.Activity = value; }
		}

		/// <summary>
		/// Sets the current progress information.
		/// </summary>
		/// <param name="currentValue">Progress current value, from 0 to the maximum.</param>
		/// <param name="maximumValue">Progress maximum value, positive or 0.</param>
		/// <remarks>
		/// This method is thread safe and designed for jobs.
		/// </remarks>
		public void SetProgressValue(double currentValue, double maximumValue)
		{
			_progress.SetProgressValue(currentValue, maximumValue);
		}

		/// <summary>
		/// It is not used directly.
		/// </summary>
		public void ShowProgress()
		{ }

		#endregion
	}
}
