
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;

namespace FarNet.Forms
{
	/// <summary>
	/// Common UI event handlers.
	/// </summary>
	public static class Events
	{
		/// <summary>
		/// .
		/// </summary>
		/// <param name="sender">.</param>
		/// <param name="e">.</param>
		public static void Coloring_EditAsConsole(object sender, ColoringEventArgs e)
		{
			// normal text
			e.Background1 = ConsoleColor.Black;
			e.Foreground1 = ConsoleColor.Gray;
			// selected text
			e.Background2 = ConsoleColor.White;
			e.Foreground2 = ConsoleColor.Black;
			// unchanged text
			e.Background3 = ConsoleColor.Black;
			e.Foreground3 = ConsoleColor.Gray;
			// combo
			e.Background4 = ConsoleColor.Black;
			e.Foreground4 = ConsoleColor.Gray;
		}

		/// <summary>
		/// .
		/// </summary>
		/// <param name="sender">.</param>
		/// <param name="e">.</param>
		public static void Coloring_TextAsConsole(object sender, ColoringEventArgs e)
		{
			// normal text
			e.Background1 = ConsoleColor.Black;
			e.Foreground1 = ConsoleColor.Gray;
		}

		/// <summary>
		/// .
		/// </summary>
		/// <param name="sender">.</param>
		/// <param name="e">.</param>
		public static void MouseClicked_IgnoreOutside(object sender, MouseClickedEventArgs e)
		{
			if (e.Control == null)
				e.Ignore = true;
		}
	}

	/// <summary>
	/// Base class of dialog and control event arguments.
	/// </summary>
	public class AnyEventArgs : EventArgs
	{
		/// <param name="control">Control involved into this event or null.</param>
		public AnyEventArgs(IControl control)
		{
			Control = control;
		}
		/// <summary>
		/// Event's control or null. See the constructor for details.
		/// </summary>
		public IControl Control { get; }
	}

	/// <summary>
	/// <see cref="IDialog.Initialized"/> event arguments.
	/// </summary>
	public sealed class InitializedEventArgs : AnyEventArgs
	{
		/// <param name="focused">Control that will initially receive focus.</param>
		public InitializedEventArgs(IControl focused)
			: base(focused)
		{ }
		/// <summary>
		/// Ingore changes.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// <see cref="IDialog.Closing"/> event arguments.
	/// </summary>
	public sealed class ClosingEventArgs : AnyEventArgs
	{
		/// <param name="selected">Control that had the keyboard focus when [CtrlEnter] was pressed or the default control.</param>
		public ClosingEventArgs(IControl selected)
			: base(selected)
		{ }
		/// <summary>
		/// Ingore and don't close the dialog.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// <see cref="IControl.Drawing"/> event arguments.
	/// </summary>
	public sealed class DrawingEventArgs : AnyEventArgs
	{
		/// <param name="control">Control that is about to be drawn.</param>
		public DrawingEventArgs(IControl control)
			: base(control)
		{ }
		/// <summary>
		/// Ingore and don't draw the control.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// <see cref="IControl.Drawn"/> event arguments.
	/// </summary>
	public sealed class DrawnEventArgs : AnyEventArgs
	{
		/// <param name="control">Control that is drawn.</param>
		public DrawnEventArgs(IControl control)
			: base(control)
		{ }
	}

	/// <summary>
	/// <see cref="IControl.Coloring"/> event arguments.
	/// </summary>
	/// <remarks>
	/// Event handlers change the default colors provided by the event arguments.
	/// There are up to 4 color pairs (foreground and background).
	/// <para>
	/// <see cref="IBox"/>: 1: Title; 2: HiText; 3: Frame.
	/// </para>
	/// <para>
	/// <see cref="IText"/>:
	/// Normal text: 1: Title; 2: HiText; 3: Frame.
	/// Vertical text: 1: Title.
	/// The box color applies only to text items with the <see cref="IText.Separator"/> flag set.
	/// </para>
	/// <para>
	/// <see cref="IEdit"/>, <see cref="IComboBox"/>: 1: EditLine; 2: Selected Text; 3: Unchanged Color; 4: History and ComboBox pointer.
	/// </para>
	/// <para>
	/// <see cref="IButton"/>, <see cref="ICheckBox"/>, <see cref="IRadioButton"/>: 1: Title; 2: HiText.
	/// </para>
	/// <para>
	/// <see cref="IListBox"/> recieves another event which is not yet exposed by FarNet.
	/// </para>
	/// </remarks>
	public sealed class ColoringEventArgs : AnyEventArgs
	{
		/// <param name="control">Control to set colors for.</param>
		public ColoringEventArgs(IControl control)
			: base(control)
		{ }
		/// <summary>
		/// Color 1, foreground.
		/// </summary>
		public ConsoleColor Foreground1 { get; set; }
		/// <summary>
		/// Color 1, background.
		/// </summary>
		public ConsoleColor Background1 { get; set; }
		/// <summary>
		/// Color 2, foreground.
		/// </summary>
		public ConsoleColor Foreground2 { get; set; }
		/// <summary>
		/// Color 2, background.
		/// </summary>
		public ConsoleColor Background2 { get; set; }
		/// <summary>
		/// Color 3, foreground.
		/// </summary>
		public ConsoleColor Foreground3 { get; set; }
		/// <summary>
		/// Color 3, background.
		/// </summary>
		public ConsoleColor Background3 { get; set; }
		/// <summary>
		/// Color 4, foreground.
		/// </summary>
		public ConsoleColor Foreground4 { get; set; }
		/// <summary>
		/// Color 4, background.
		/// </summary>
		public ConsoleColor Background4 { get; set; }
	}

	/// <summary>
	/// <see cref="IControl.LosingFocus"/> event arguments.
	/// </summary>
	public sealed class LosingFocusEventArgs : AnyEventArgs
	{
		/// <param name="losing">Control losing focus.</param>
		public LosingFocusEventArgs(IControl losing)
			: base(losing)
		{ }
		/// <summary>
		/// Control you want to pass focus to or leave it null to allow losing focus.
		/// </summary>
		public IControl Focused { get; set; }
	}

	/// <summary>
	/// <c>ButtonClicked</c> event arguments for <see cref="IButton"/>, <see cref="ICheckBox"/>, <see cref="IRadioButton"/>.
	/// </summary>
	public sealed class ButtonClickedEventArgs : AnyEventArgs
	{
		/// <param name="button">Button clicked.</param>
		/// <param name="selected">Selected state.</param>
		public ButtonClickedEventArgs(IControl button, int selected)
			: base(button)
		{
			Selected = selected;
		}
		/// <summary>
		/// Selected state:
		/// <see cref="IButton"/>: 0;
		/// <see cref="ICheckBox"/>: 0 (unchecked), 1 (checked) and 2 (undefined for ThreeState);
		/// <see cref="IRadioButton"/>: 0 - for the previous element in the group, 1 - for the active element in the group.
		/// </summary>
		public int Selected { get; }
		/// <summary>
		/// The message has been handled and it should not be processed by the kernel.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// <c>TextChanged</c> event arguments for <see cref="IEdit"/>, <see cref="IComboBox"/>.
	/// </summary>
	public sealed class TextChangedEventArgs : AnyEventArgs
	{
		/// <param name="edit">Edit control.</param>
		/// <param name="text">New text.</param>
		public TextChangedEventArgs(IControl edit, string text)
			: base(edit)
		{
			Text = text;
		}
		/// <summary>
		/// New text.
		/// </summary>
		public string Text { get; }
		/// <summary>
		/// Ignore changes.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// <c>KeyPressed</c> event arguments for <see cref="IDialog"/> and <see cref="IControl"/>.
	/// </summary>
	public sealed class KeyPressedEventArgs : AnyEventArgs
	{
		/// <param name="control">Current control.</param>
		/// <param name="key">The key.</param>
		public KeyPressedEventArgs(IControl control, KeyInfo key)
			: base(control)
		{
			Key = key;
		}
		/// <summary>
		/// The key.
		/// </summary>
		public KeyInfo Key { get; }
		/// <summary>
		/// Ignore further processing.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// <c>MouseClicked</c> event arguments for <see cref="IDialog"/> and <see cref="IControl"/>.
	/// </summary>
	public sealed class MouseClickedEventArgs : AnyEventArgs
	{
		/// <param name="control">Current control.</param>
		/// <param name="mouse">Mouse info.</param>
		public MouseClickedEventArgs(IControl control, MouseInfo mouse)
			: base(control)
		{
			Mouse = mouse;
		}
		/// <summary>
		/// Mouse info.
		/// </summary>
		public MouseInfo Mouse { get; set; }
		/// <summary>
		/// Ignore further processing.
		/// </summary>
		public bool Ignore { get; set; }
	}

	/// <summary>
	/// Size event arguments, e.g. of <see cref="IDialog.ConsoleSizeChanged"/> event.
	/// </summary>
	public sealed class SizeEventArgs : AnyEventArgs
	{
		/// <param name="control">It is null.</param>
		/// <param name="size">The size.</param>
		public SizeEventArgs(IControl control, Point size)
			: base(control)
		{
			Size = size;
		}
		/// <summary>
		/// The size.
		/// </summary>
		public Point Size { get; set; }
	}

	/// <summary>
	/// <see cref="IDropDown.DropDownOpening"/> event arguments.
	/// </summary>
	public sealed class DropDownOpeningEventArgs : AnyEventArgs
	{
		/// <param name="control">Control which drop down is opening.</param>
		public DropDownOpeningEventArgs(IControl control)
			: base(control)
		{ }
	}

	/// <summary>
	/// <see cref="IDropDown.DropDownClosed"/> event arguments.
	/// </summary>
	public sealed class DropDownClosedEventArgs : AnyEventArgs
	{
		/// <param name="control">Control which drop down is closed.</param>
		public DropDownClosedEventArgs(IControl control)
			: base(control)
		{ }
	}
}
