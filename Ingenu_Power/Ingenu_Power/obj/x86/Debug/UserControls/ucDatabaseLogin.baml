   M S B A M L   `   `   `  ����G  CIngenu_Power, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullE'clr-namespace:Ingenu_Power.UserControlsIngenu_Power.UserControls  5      9!clr-namespace:Ingenu_Power.DomainIngenu_Power.Domain  5      O Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem 5	      /    )Ingenu_Power.UserControls.UcDatabaseLogin   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      9local'clr-namespace:Ingenu_Power.UserControls     5      U QMaterialDesignThemes.Wpf, Version=3.0.1.920, Culture=neutral, PublicKeyToken=nullFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5      .domain!clr-namespace:Ingenu_Power.Domain  5      3system&clr-namespace:System;assembly=mscorlib  5	      -   5          � 	Resources  5       %  !InputBoxHintAssistForegorundBrush �� Color  	InputIcon �� 
TargetType'  !MaterialDesignThemes.Wpf.PackIcon �� Property �� Value  SkyBrush �� 
StartPoint �� EndPoint � Color � Offset  GroundBrush��%�  &	        &	       &	 �     &	 �    �� 5      $ Purple��6>   �� 5      "  6   .�5      ��  � ��$ 30��6   �� 5       � ��$ 30��6   �� 5       � ��$ Center=�6+   �� 5       � ��$ Center=�6)   5      �� 5      $	 0 0&�6)   $	 0 1&�6:   .��5      � $ #2196F3��6   $ 0Q�6"   � 5      $ #2196F3��6   $ .6Q�6"   � 5      $ #00E676��6   $ .6Q�6"   � 5      $ #00E676��6   $ 1Q�6"   5      �� 5      $	 0 0&�6,   $	 0 1&�6=   .��5      � $ 	#552196F3��6   $ 0Q�6$   � 5      $ #FFFFFF��6   $ .3Q�6"   � 5      $ #00E676��6   $ 1Q�6"   5      5      .��5!      � $	��443��6	   $	��726��6   $��Center=�6"   $��Center=�6?   $��2��6Z   .+�5#      #  MaterialDesignThemes.Wpf.Card  $��0��6   $��2��6%   $��Center=�66   $��Center=�6S   $	��360��6n   $	��266��6z   $��10q�6�   .��5$      �� $��0 0 0 0q�6   .+�5%      � $
��0 10q�6   	 � ColumnDefinitions	 5&   	   �� 5'   
   $��30 �6   �� 5(   
   $��30 �6   �� 5)   
   $��* �6   �� 5*   
   $��30 �6   .+�5,   	     
   Kind$
 Database=�6"   #��[ 62   $��40��6U   $��40��6`    J� RenderTransformOrigin$ 0.289,0.565&�6l   $��2��6�   $��19,0,0,0q�6�   �� 5-   	   -   5         ��Name$ 
TxtSQLName��5-   X   $��2��6   $
��10 0q�6!   )  #MaterialDesignThemes.Wpf.HintAssist   Hint$ SQL-IP��60      
Foreground# [  5.   "   .  (MaterialDesignThemes.Wpf.TextFieldAssist   UnderlineBrush# [  5/   "      FloatingScale$	 0.6Q�50   "    % !MaterialDesignFloatingHintTextBox#��[ 51   "   $��20(�52   "   $��6,0,0,0q�60   .��6B   53   
   54   	   � 56      $
��0 10q�6   	 57   	   �� 58   
   $��30 �6   �� 59   
   $��30 �6   �� 5:   
   $��* �6   �� 5;   
   $��30 �6   .+�5=   	     $
 Account=�6"   #��[ 61   $��40��6T   $��40��6_   $ 0.289,0.565&�6k   $��2��6�   $��19,0,0,0q�6�   �� 5>   	   -   5        $ TxtUserName��5>   X   $��2��6   $
��10 0q�6!   $ 
SQL-用户��60   # [  5?   "   # [  5@   "   $	 0.6Q�5A   "   #��[ 5B   "   $��20(�5C   "   $��6,0,0,0q�60   .��6B   .��5K      5L   
   5M   	   � 5O      $
��0 10q�6   	 5P   	   �� 5Q   
   $��30 �6   �� 5R   
   $
��Auto �6   �� 5S   
   �� 5T   
   $��30 �6   .+�5V   	     $	
 Key=�6!   #��[ 6,   $��40��6O   $��2��6[   $��19,0,0,0q�6o   $��40��6�   G� 5W   	   -   5         G�Name$ FloatingPasswordBox��5X   &   $��2��5W      $
��10 0q�6%   $ 
SQL-密码��5Y   &   # [  5Z   &   # [  5[   &   $	 0.6Q�5\   &    ) %MaterialDesignFloatingHintPasswordBox#��[ 5]   &   $��20(�5^   &   5_   	   5`      5a      � 5c      $��50��6	   $	��Top=�6   $��Center=�6-   $	��120��6J   $��294,330,293,0q�6V   .+�5d      �� -   5        $��BtnLogin��5d       �� 	IsDefault . $��23(�5e       " MaterialDesignRaisedDarkButton#��[ 5f      $
��Auto��5g      +  %MaterialDesignThemes.Wpf.ButtonAssist   CornerRadius$ 25��5h      .��5i      � .+�5j        $
 Database=�6   $��30��6/   $��30��6;   �� 5k      $��登录��6   5l      5n      5o      5q      5s      