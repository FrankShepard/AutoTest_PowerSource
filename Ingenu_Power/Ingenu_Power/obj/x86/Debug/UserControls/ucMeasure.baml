   M S B A M L   `   `   `  ����G  CIngenu_Power, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullE'clr-namespace:Ingenu_Power.UserControlsIngenu_Power.UserControls  5      9!clr-namespace:Ingenu_Power.DomainIngenu_Power.Domain  5      )    #Ingenu_Power.UserControls.UcMeasure   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      9local'clr-namespace:Ingenu_Power.UserControls     5      U QMaterialDesignThemes.Wpf, Version=3.0.1.920, Culture=neutral, PublicKeyToken=nullFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5      .domain!clr-namespace:Ingenu_Power.Domain  5      h dNationalInstruments.Controls, Version=15.0.40.3898, Culture=neutral, PublicKeyToken=dc6ad606294fc298q mNationalInstruments.Controls.Booleans, Version=15.0.40.3898, Culture=neutral, PublicKeyToken=dc6ad606294fc298@ni5http://schemas.ni.com/controls/2009/xaml/presentation   5	      .��5      � .+�5      � $	��443��6	   $	��726��6   $��Center=�6"   $��Center=�6?   .+�5      #  MaterialDesignThemes.Wpf.Card  $��0��6   $��Center=�6&   $��Center=�6C   $	��706��6^   $	��423��6j   $��10q�6w   $��20q�6�   .��5      �   � RowDefinitions  5      �� 5      �� 5      $��30 �6   .+�5      � $��0��6    � ColumnDefinitions 5      �� 5   	   �� 5   	   .+�5      � $��0��6     5   	   �� 5   
   $��2* �6   �� 5   
   $��* �6   �� 5   
   $��2* �6   �� 5    
   $��* �6   �� 5!   
   $��* �6   .+�5#   	   �� $��0��6   $0��*/Ingenu_Power;component/Resources/logo.jpg��6   $��80��6P   $��5q�6\   � 5%   	   $��1��6   $��10,0,0,0q�6    5&   
   �� 5'      $��60 �6   �� 5(      $��* �6   .+�5*   
   '  !MaterialDesignThemes.Wpf.PackIcon     Kind$ BarcodeScan=�6"   $��50��65   $��45��6@   $��1��6L   $��5,0,0,0q�6`   $��Center=�6q   �� 5+   
   -   5        $��TxtID��5+     $
��20,0q�6   )  #MaterialDesignThemes.Wpf.HintAssist   Hint$ 产品ID��6     %  !MaterialDesignFloatingHintTextBox#��[  6F   $��2��6�   $��Center=�6�    �� 	MaxLength$ 15��6  6  5,   
   � 5.   	   $��2��6   .+�5/   
   �� $��工序确认��6     MaterialDesignGroupBox#���  6!   $��15,10q�6R   +  %MaterialDesignThemes.Wpf.ShadowAssist   ShadowDepth$ Depth3=�6a   $��0q�6�   .��50      �  51      �� 52      $��* �6   �� 53      $��* �6   �� 54      $��* �6   .+�57      � $��0��6   $��Center=�6!   .+�58      �� $��0��6   $��    程序烧录��6#   $��0q�66   $��12(�6A   $��Center=�6O   $
��Left=�6j   �� 59      -   5         ��Name$ chkISP��59   L   $��0��6   $��              ��6&   $��0q�6A     MaterialDesignCheckBox#��[ 6\   $
��Left=�6�   $��Center=�6�   5:      � 5;      $��1��6   $��Center=�6!   .+�5<      �� $��0��6   $��    产品校准��6#   $��0q�66   $��12(�6A   $��Center=�6O   $
��Left=�6j   �� 5=      -   5        $ chkCalibrate��5=   L   $��0��6   $��              ��6&   $��0q�6A   #��[ 6b   $
��Left=�6�   $��Center=�6�   5>      � 5?      $��2��6   $��Center=�6!   .+�5@      �� $��0��6   $��    全项测试��6#   $��0q�66   $��12(�6A   $��Center=�6O   $
��Left=�6j   �� 5A      -   5        $ chkWholeFunctionTest��5A   L   $��0��6   $��              ��6&   $��0q�6A   #��[ 6j   $
��Left=�6�   $��Center=�6�   5B      5C      5D      5E   
   � 5G   	   $��3��6   .+�5H   
   �� -   5         ��Name$ 
BtnMeasure��5H   "    �� 	IsDefault . $��25(�6P    " MaterialDesignRaisedDarkButton#��[ 6^   $
��auto��6�   $	��200��6�   +  %MaterialDesignThemes.Wpf.ButtonAssist	   CornerRadius$	 25��5I   
   $ Depth4=�68   $��3q�6i   .��5J      � .+�5K        $ 
GooglePlay=�6$   $��0q�66   $��32��6A   $��32��6M   �� 5L      $��一键测试��6   5M      5N      5O   
   5Q   	   � 5R      $��1��6   .+�5S   	   �� $��10,30,10,10q�6   .+�5T   
   � .+�5U      �� -   5        $��ChkMeasureIgnoreFault��5U      $��异常继续测试��61   (  "MaterialDesignThemes.Wpf.RatingBar  5V      -   5        
  Name$
 BasicRatingBar��5V   6   
   Max$ 8��6$      Value$ 1��6,    � ToolTip$* $测试过程中的延时时长等级��6N   $��10q�6e   $��Right=�6q   5W      � 5X   
   .+�5Y        $ Creation=�6#   $��30��63   $��30��6?   $��Center=�6J   �� 5Z      $��执行环节��6   $��22(�6!   $��Italic$�6/   $
��Left=�6B   $��Center=�6]   $��5q�6x   $
��Bold"�6�   �� 5[      -   5         ��Name$ TxtLink��5[   {   $�� ��6   $��16(�6   $
��Left=�6+   $��Center=�6F   $��5q�6a    �� Padding$
 25,5q�6l   6{   5\      �� 5]   
     MaterialDesignDarkSeparator#��[ 6   $	��8 0q�6I   � 5^   
   .+�5_        $ Gauge=�6#   $��30��60   $��30��6<   $��Center=�6G   �� 5`      $��	测试项��6   $��22(�6    $��Italic$�6.   $
��Left=�6A   $��Center=�6\   $��5q�6w   $
��Bold"�6�   �� 5a      -	   5        $ TxtMeasuredItem��5a   {   $�� ��6   $��16(�6   $
��Left=�6+   $��Center=�6F   $��5q�6a   $
 25,5q�6l   6{   5b      �� 5c   
   #��[ 6   $	��8 0q�6I   � 5d   
   .+�5e        $ Counter=�6#   $��30��62   $��30��6>   $��Center=�6I   �� 5f      $��	测试值��6   $��22(�6    $��Italic$�6.   $
��Left=�6A   $��Center=�6\   $��5q�6w   $
��Bold"�6�   �� 5g      -
   5        $ TxtMeasuredResult��5g   {   $�� ��6   $��16(�6   $
��Left=�6+   $��Center=�6F   $��5q�6a   $
 25,5q�6l   6{   5h      5i   
   &   NationalInstruments.Controls.LED  5j   	   -   5          Name$	 Led��5j      $	��110��6   $	��110��6*   $��197,247,36,16q�66   0  *NationalInstruments.Controls.BooleanButton   Value .     
IsReadOnly . 6[   5k   	   5l      � 5m      $��1��6   .+�5n      � -   5        $��prgStep��5n   8    � Value$ 0Q�6   $
��20,5q�6   $��10��6,   68   5o      5p      5q      5s      5t      5u      