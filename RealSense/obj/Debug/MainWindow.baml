   M S B A M L   `   `   `  ����  	RealSense    RealSense.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  
MainWindow��5   	   $	��760��6   $
��1024��6)   .��5      � $��0,0,0.4,-0.2q�6    � RowDefinitions 5   
   �� 5       � ColumnDefinitions 5	   
   �� 5
      .+�5   
   �� -   5         ��Name$ imgColorStream��5      $��163,77,166.6,291.4q�6(    J� RenderTransformOrigin$ 0.502,0.474&�6D   $
��Fill=�6h   �� 5   
   -   5         ��Name$ 
lblMessage��5      $��(Wave Your Hand!)��6$   $��179,-64,434.6,766.4q�6@   $��Bottom=�6]   $��Center=�6x   $ 0.552,13.191&�6�   $	��404��6�   $��28��6�   �� 5   
   -   5        $ lblFacesDetected��5      $��Faces Detected: 0��6*   $
��Left=�6F   $��40,462,0,236.4q�6a   $	��171��6y   �� 5   
   -   5        $ lblLEyeClosedIntensity��5      $"��Left Eye Closed Intensity: 0��60   $
��Left=�6W   $��40,494,0,203.4q�6r   $	��171��6�   �� 5   
   -   5        $ lblLEyeClosed��5      $��Left Eye Closed: No��6'   $
��Left=�6E   $��40,527,0,171.4q�6`   $	��171��6x   �� 5   
   -   5        $ lblREyeClosedIntensity��5      $#��Right Eye Closed Intensity: 0��60   $��40,559,806.6,129.4q�6X   $ 0.5,0.5&�6t   �� 5   
   -   5        $ lblREyeClosed��5      $��Right Eye Closed: Yes��6'   $��40,593,829.6,106.4q�6G   $ 0.5,0.5&�6c   �� 5   
   -   5        $ 
lblEmotion��5      $��
Emotion: ?��6$   $��40,624,829.6,74.4q�69   $ 0.5,0.5&�6T   �� 5   
   -	   5        $ lblEmotionEvidence��5      $��40,656,829.6,42.4q�6,   $ 0.5,0.5&�6G   $��Emotion Evidence: ��6g   �� 5   
   -
   5        $ lblBlink��5      $��Blink Detected: No��6"   $��40,688,829.6,11.4q�6?   $ 0.5,0.5&�6Z   �� 5   
   -   5        $ lblFPS��5      $��FPS: 0��6    $��0,444,344.6,259.4q�61   $ 0.5,0.5&�6L   $��27��6l   $��Right=�6x   $	��236��6�   �� 5   
   -   5         ��Name$ cbExprSelect��5   `    �� 
IsEditable .  �� 
IsReadOnly . 	 �� Text$'	 !Select Expression to Inspect Live��67   $��24��6v   $��366,480,385.6,0q�6�   $	��Top=�6�   6�   �� 5   
   -   5        $ lblExprIntensity��5      $��Expression Intensity: 0��6*   $
��Left=�6L   $��366,511,0,180.4q�6g   $	��269��6�   �� 5   
   -   5        $ lblBoundingRect��5      $��Bounding Rect: ��6)   $
��Left=�6C   $��24��6^   $��366,577,0,0q�6j   $	��Top=�6   $	��485��6�   �� 5   
   -   5        $ lblAverageDepth��5      $��Average Depth: ��6)   $
��Left=�6C   $��24��6^   $��366,606,0,0q�6j   $	��Top=�6   $	��181��6�   �� 5   
   -   5        $ lblPoseEulerAngles��5      $��Pose Euler Angles: ��6,   $
��Left=�6J   $��46��6e   $��632,610,0,0q�6q   $	��Top=�6�   $	��368��6�   �� 5   
   $��371,633,395.6,0q�6   $	��Top=�60   $
��Auto=�6H   .��5      �� -   5        
 ��Name$
 textBlockLandmarkPoints��5      $
��Wrap=�69   $��	TextBlock��6M   5      �� 5    
   -   5        $ cbCameraMode��5        .  . $	 Select Camera Mode��6M   $
��Left=�6g   $��26��6�   $��
373,10,0,0q�6�   $	��Top=�6�   $	��255��6�   6�   �� 5!   
   -   5        $ lblPoseQuaternionAngles��5!      $��Pose Euler Angles: ��61   $
��Left=�6O   $��46��6j   $��632,661,0,0q�6v   $	��Top=�6�   $	��368��6�   5#      5$      