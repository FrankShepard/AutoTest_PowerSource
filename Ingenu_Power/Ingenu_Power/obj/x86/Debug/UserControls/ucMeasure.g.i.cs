﻿#pragma checksum "..\..\..\..\UserControls\ucMeasure.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "D2C7E3A5E6A29CE9143C588EAE06EE3753440C61"
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
using NationalInstruments.Controls;
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


namespace Ingenu_Power.UserControls {
    
    
    /// <summary>
    /// UcMeasure
    /// </summary>
    public partial class UcMeasure : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 43 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox TxtID;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkISP;
        
        #line default
        #line hidden
        
        
        #line 61 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkCalibrate;
        
        #line default
        #line hidden
        
        
        #line 65 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkWholeFunctionTest;
        
        #line default
        #line hidden
        
        
        #line 72 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtnMeasure;
        
        #line default
        #line hidden
        
        
        #line 84 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider SldMagnification;
        
        #line default
        #line hidden
        
        
        #line 89 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TxtLink;
        
        #line default
        #line hidden
        
        
        #line 95 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TxtMeasuredItem;
        
        #line default
        #line hidden
        
        
        #line 101 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TxtMeasuredResult;
        
        #line default
        #line hidden
        
        
        #line 104 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal NationalInstruments.Controls.LED Led;
        
        #line default
        #line hidden
        
        
        #line 108 "..\..\..\..\UserControls\ucMeasure.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar prgStep;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Ingenu_Power;component/usercontrols/ucmeasure.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\UserControls\ucMeasure.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.TxtID = ((System.Windows.Controls.TextBox)(target));
            
            #line 43 "..\..\..\..\UserControls\ucMeasure.xaml"
            this.TxtID.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.TextBox_PreviewKeyDown);
            
            #line default
            #line hidden
            
            #line 43 "..\..\..\..\UserControls\ucMeasure.xaml"
            this.TxtID.PreviewMouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(this.TextBox_PreviewMouseDoubleClick);
            
            #line default
            #line hidden
            return;
            case 2:
            this.chkISP = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 3:
            this.chkCalibrate = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 4:
            this.chkWholeFunctionTest = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 5:
            this.BtnMeasure = ((System.Windows.Controls.Button)(target));
            
            #line 72 "..\..\..\..\UserControls\ucMeasure.xaml"
            this.BtnMeasure.Click += new System.Windows.RoutedEventHandler(this.BtnMeasure_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.SldMagnification = ((System.Windows.Controls.Slider)(target));
            return;
            case 7:
            this.TxtLink = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 8:
            this.TxtMeasuredItem = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 9:
            this.TxtMeasuredResult = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 10:
            this.Led = ((NationalInstruments.Controls.LED)(target));
            return;
            case 11:
            this.prgStep = ((System.Windows.Controls.ProgressBar)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

