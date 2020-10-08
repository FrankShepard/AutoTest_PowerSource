using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;

namespace Ingenu_Power.UserControls
{
	/// <summary>
	/// ucAdvancedSettings.xaml 的交互逻辑
	/// </summary>
	public partial class ucAdvancedSettings : UserControl
	{
		public ucAdvancedSettings()
		{
			InitializeComponent();
			if ( Properties.Settings.Default.明暗主题_dark ) {
                TogDark.IsChecked = true;
            }
        }

        /// <summary>
        /// 判断明暗主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void ToggleButton_Click( object sender , RoutedEventArgs e )
		{
            bool isDark = false;
            ToggleButton toggleButton = sender as ToggleButton;
			if ( ( bool ) toggleButton.IsChecked ) {
                isDark = true;
            }
            ChooseLightOrDark( isDark );
        }

        /// <summary>
        /// 具体选择明暗主题
        /// </summary>
        /// <param name="is_dark"></param>
        public void ChooseLightOrDark(bool is_dark)
		{
            //资源字典中是否存在主题的判断；若是之前存在主题，则需要将其替换   资源字典在 App.xaml中声名
            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                .Where( rd => rd.Source != null )
                .SingleOrDefault( rd => Regex.Match( rd.Source.OriginalString , @"(\/MaterialDesignThemes.Wpf;component\/Themes\/MaterialDesignTheme\.)((Light)|(Dark))" ).Success );
            if ( existingResourceDictionary == null )
                throw new ApplicationException( "Unable to find Light/Dark base theme in Application resources." );

            var source =
                $"pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.{( is_dark ? "Dark" : "Light" )}.xaml";
            var newResourceDictionary = new ResourceDictionary( ) { Source = new Uri( source ) };

            Application.Current.Resources.MergedDictionaries.Remove( existingResourceDictionary );
            Application.Current.Resources.MergedDictionaries.Add( newResourceDictionary );

            Properties.Settings.Default.明暗主题_dark = is_dark;
            Properties.Settings.Default.Save( );
        }

        /// <summary>
        /// 获取具体的主题颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void Button_Click( object sender , RoutedEventArgs e )
		{
            Button button = sender as Button;
            string new_color = button.Content.ToString();
            ChoosePalette( new_color );
        }

        /// <summary>
        /// 具体调整主题颜色的代码
        /// </summary>
        /// <param name="new_color"></param>
        public void ChoosePalette(string new_color )
		{
            //资源字典中是否存在主题的判断；若是之前存在主题，则需要将其替换   资源字典在 App.xaml中声名
            var existingResourceDictionary = Application.Current.Resources.MergedDictionaries
                .Where( rd => rd.Source != null )
                .SingleOrDefault( rd => Regex.Match( rd.Source.OriginalString , @"(\/MaterialDesignColors;component\/Themes\/Recommended\/Primary\/MaterialDesignColor\.)((DeepPurple)|(Yellow)|(LightBlue)|(Teal)|(Cyan)|(Pink)|(Green)|(Indigo)|(LightGreen)|(Blue)|(Lime)|(Red)|(Orange)|(Purple)|(Grey)|(Brown))" ).Success );
            if ( existingResourceDictionary == null )
                throw new ApplicationException( "Unable to find Light/Dark base theme in Application resources." );

            var source =
                $"pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor." + new_color + ".xaml";
            var newResourceDictionary = new ResourceDictionary( ) { Source = new Uri( source ) };

            Application.Current.Resources.MergedDictionaries.Remove( existingResourceDictionary );
            Application.Current.Resources.MergedDictionaries.Add( newResourceDictionary );

            Properties.Settings.Default.Palette = new_color;
            Properties.Settings.Default.Save( );
        }

    }
}
