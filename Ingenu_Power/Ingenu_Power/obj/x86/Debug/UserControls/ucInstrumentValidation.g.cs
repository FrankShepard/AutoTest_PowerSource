#pragma checksum "..\..\..\..\UserControls\ucInstrumentValidation.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "E1D88800CA487FF165D3AD402736F79AC5539FC4"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using Ingenu_Power.Domain;
using Ingenu_Power.UserControls;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Converters;
using MaterialDesignThemes.Wpf.Transitions;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Ingenu_Power.UserControls
{


	/// <summary>
	/// UcInstrumentValidation
	/// </summary>
	public partial class UcInstrumentValidation : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector
	{


#line 59 "..\..\..\..\UserControls\ucInstrumentValidation.xaml"
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute( "Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields" )]
		internal System.Windows.Controls.Button BtnValidate;

#line default
#line hidden

		private bool _contentLoaded;

		/// <summary>
		/// InitializeComponent
		/// </summary>
		[System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[System.CodeDom.Compiler.GeneratedCodeAttribute( "PresentationBuildTasks", "4.0.0.0" )]
		public void InitializeComponent()
		{
			if (_contentLoaded) {
				return;
			}
			_contentLoaded = true;
			System.Uri resourceLocater = new System.Uri( "/Ingenu_Power;component/usercontrols/ucinstrumentvalidation.xaml", System.UriKind.Relative );

#line 1 "..\..\..\..\UserControls\ucInstrumentValidation.xaml"
			System.Windows.Application.LoadComponent( this, resourceLocater );

#line default
#line hidden
		}

		[System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[System.CodeDom.Compiler.GeneratedCodeAttribute( "PresentationBuildTasks", "4.0.0.0" )]
		[System.ComponentModel.EditorBrowsableAttribute( System.ComponentModel.EditorBrowsableState.Never )]
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute( "Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes" )]
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute( "Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity" )]
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute( "Microsoft.Performance", "CA1800:DoNotCastUnnecessarily" )]
		void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId) {
				case 1:

#line 42 "..\..\..\..\UserControls\ucInstrumentValidation.xaml"
					(( System.Windows.Controls.ComboBox )(target)).PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler( this.ComboBox_PreviewMouseDown );

#line default
#line hidden
					return;
				case 2:
					this.BtnValidate = (( System.Windows.Controls.Button )(target));

#line 59 "..\..\..\..\UserControls\ucInstrumentValidation.xaml"
					this.BtnValidate.Click += new System.Windows.RoutedEventHandler( this.BtnValidate_Click );

#line default
#line hidden
					return;
			}
			this._contentLoaded = true;
		}

		internal System.Windows.Controls.ProgressBar PgbStep;
		internal System.Windows.Controls.ComboBox CobSp;
		internal MaterialDesignThemes.Wpf.PackIcon pckConnect;
		internal System.Windows.Controls.TextBox TxtINS;
	}
}

