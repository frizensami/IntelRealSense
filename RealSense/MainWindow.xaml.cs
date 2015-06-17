using System;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.IO;
using NamedPipeWrapper;


namespace RealSense
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //File names
        private string BLINK_FILE_NAME = "facedata.csv";
        private System.IO.StreamWriter file;

        private Thread processingThread;
        private PXCMSenseManager senseManager;



        //Things to do with hands
        private PXCMHandModule hand;
        private PXCMHandConfiguration handConfig;
        private PXCMHandData handData;
        private PXCMHandData.GestureData gestureData;

        //Things to do with faces
        private PXCMFaceModule face;
        private PXCMFaceConfiguration faceConfig;
        private PXCMFaceConfiguration.ExpressionsConfiguration exprConfig;
        private PXCMFaceData faceData;

        //Things to do with emotions
        private PXCMEmotion emotion;
        private PXCMEmotion.Emotion currentEmotion;
        private int emotionEvidence;

        //Overall expressions array
        private Hashtable exprTable;

        //fps vars
        Stopwatch stopwatch;
        private double prevTime;
        private double fps;

        //global triggers and true-false vars
        private bool handWaving;
        private bool handTrigger;
        private int handResetTimer;

        private bool lEyeClosed;
        private bool lEyeClosedTrigger;
        private int lEyeClosedResetTimer;

        private bool rEyeClosed;
        private bool rEyeClosedTrigger;
        private int rEyeClosedResetTimer;

        private bool blinkTrigger;
        private int blinkResetTimer;

        //non bool vars for labels
        private int numFacesDetected;
        private int lEyeClosedIntensity;
        private int rEyeClosedIntensity;



        //checking which expression to display in the lblExprIntensity
        private string exprToDisplay;

        //checking which camera mode to use
        private string cameraMode;

        //global vars for face location data
        private PXCMRectI32 boundingRect;
        private float averageDepth;

        //global var for face landmark data points
        private PXCMFaceData.LandmarkPoint[] landmarkPoints;

        //global var for face pose data
        private PXCMFaceData.PoseEulerAngles eulerAngles;
        private PXCMFaceData.PoseQuaternion quaternionAngles;

        //global constants
        //Stream Width and Height
        private const int STREAM_WIDTH = 640;
        private const int STREAM_HEIGHT = 480;
        private const int STREAM_FPS = 30;

        private const int HAND_TIMER_RESET_FRAMES = 50;
        private const int EYE_TIMER_RESET_FRAMES = 10;
        private const int BLINK_TIMER_RESET_FRAMES = 10;

        private const int EYE_CLOSED_DETECT_THRESHOLD = 80;
        private const int LANDMARK_POINTS_TOTAL = 78;

        private const int MSEC_BETWEEN_BITMAP_SAVES = 100;

        //first frame checker
        private bool firstFrame = true;
        //timers
        private HiPerfTimer highPerformanceTimer;
        private double totalHighPerfTimeElapsed;
        private int numLinesWritten;

        //time as of starting
        private string currentDateTime;

        ////pipe stuff
        //private PipeClientServer.PipeClient _pipeClient;
        //private PipeClientServer.PipeServer _pipeServer;
        MyClient pipeClient;


        private const string PIPE_NAME = "DataCapturePipe";
        private const string CAMERA_CONNECTED_MESSAGE = "-1";

        //stimcode stuff
        public static int stimcode = 0;
        
        public MainWindow()
        {
            InitializeComponent();

            //set the current date and time
            currentDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            //set total timer count to 0 and init vars
            highPerformanceTimer = new HiPerfTimer();
            totalHighPerfTimeElapsed = 0;
            numLinesWritten = 0; //set the total number of lines written to 0 so we can track when to start the timer

            //init pipe stuff
            pipeClient = new MyClient(PIPE_NAME);
            pipeClient.SendMessage("I Am Intel RealSense");
            
           
            //Debug.WriteLine("Server Ready");

            //initialise combobox
            populateComboBox();
            //init the exprToDisplay global var
            exprToDisplay = "";

            //Work on the file

            //create paths
            string dirToCreate = "data";
            string dirToCreateFull = System.IO.Path.GetFullPath(dirToCreate);
            Directory.CreateDirectory(dirToCreateFull);


            dirToCreate = "video";
            dirToCreateFull = System.IO.Path.GetFullPath(dirToCreate);
            Directory.CreateDirectory(dirToCreateFull);

            //create the csv file to write to
            file = new StreamWriter("data/" + currentDateTime + "data" + ".csv");

           

            //initialise global expressions array - faster to add the keys here?
            var enumListMain = Enum.GetNames(typeof(PXCMFaceData.ExpressionsData.FaceExpression));
            exprTable = new Hashtable();
            string initLine = "";

            //Add the column schema

            //Initial line: timestamp and high prec time 
            initLine += "TIMESTAMP,HIGH_PRECISION_TIME_FROM_START,STIMCODE";

            //add all the expression data columns
            for (int i = 0; i < enumListMain.Length; i++)
            {
                exprTable.Add(enumListMain[i], 0);
                initLine += "," + enumListMain[i];


            }

            //add the bounding rectangle column
            initLine += "," + "BOUNDING_RECTANGLE_HEIGHT" + "," + "BOUNDING_RECTANGLE_WIDTH" + "," + "BOUNDING_RECTANGLE_X" + "," + "BOUNDING_RECTANGLE_Y";
            //add the average depth column
            initLine += "," + "AVERAGE_DEPTH";
            //add landmark points column
            for (int i = 0; i < LANDMARK_POINTS_TOTAL; i++)
            {
                initLine += "," + "LANDMARK_" + i + "_X";
                initLine += "," + "LANDMARK_" + i + "_Y";
            }
            //add euler angles columns
            initLine += "," + "EULER_ANGLE_PITCH" + "," + "EULER_ANGLE_ROLL" + "," + "EULER_ANGLE_YAW";
            initLine += "," + "QUATERNION_W" + "," + "QUATERNION_X" + "," + "QUATERNION_Y" + "," + "QUATERNION_Z";



            //write the initial row to the file
            file.WriteLine(initLine);

            //configure the camera mode selection box
            cbCameraMode.Items.Add("Color");
            cbCameraMode.Items.Add("IR");
            cbCameraMode.Items.Add("Depth");
            //configure initial camera mode
            cameraMode = "Color";

            //initialise global vars


            numFacesDetected = 0;

            handWaving = false;
            handTrigger = false;
            handResetTimer = 0;

            lEyeClosedIntensity = 0;
            lEyeClosed = false;
            lEyeClosedTrigger = false;
            lEyeClosedResetTimer = 0;


            rEyeClosed = false;
            rEyeClosedTrigger = false;
            rEyeClosedResetTimer = 0;
            rEyeClosedIntensity = 0;

            emotionEvidence = 0;

            blinkTrigger = false;
            blinkResetTimer = 0;

            //global fps vars
            prevTime = 0;
            stopwatch = new Stopwatch();

            // Instantiate and initialize the SenseManager
            senseManager = PXCMSenseManager.CreateInstance();
            if (senseManager == null)
            {
                MessageBox.Show("Cannot initialise sense manager: closing in 20s, report to Sriram");
                Thread.Sleep(20000);
                Environment.Exit(1);
            }
            

            
            //capture samples
            senseManager.captureManager.SetFileName("video/" + currentDateTime + ".raw", true);
            //Enable color stream
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, STREAM_WIDTH, STREAM_HEIGHT, STREAM_FPS);
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, STREAM_WIDTH, STREAM_HEIGHT, STREAM_FPS);
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_IR, STREAM_WIDTH, STREAM_HEIGHT, STREAM_FPS);
            //Enable face and hand tracking AND EMOTION TRACKING
            senseManager.EnableHand();
            senseManager.EnableFace();
            senseManager.EnableEmotion();

            //Initialise the senseManager - begin collecting data
            senseManager.Init();
 
            

            // Configure the Hand Module
            hand = senseManager.QueryHand();
            handConfig = hand.CreateActiveConfiguration();
            handConfig.EnableGesture("wave");
            handConfig.EnableAllAlerts();
            handConfig.ApplyChanges();

            //Configure the Face Module
            face = senseManager.QueryFace();
            faceConfig = face.CreateActiveConfiguration();
            faceConfig.EnableAllAlerts();
            faceConfig.detection.isEnabled = true; //enables querydetection function to retrieve face loc data
            faceConfig.detection.maxTrackedFaces = 1; //MAXIMUM TRACKING - 1 FACE
            faceConfig.ApplyChanges();
            //Configure the sub-face-module Expressions
            exprConfig = faceConfig.QueryExpressions();
            exprConfig.Enable();
            exprConfig.EnableAllExpressions();
            faceConfig.ApplyChanges();


            // Start the worker thread that processes the captured data in real-time
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            processingThread.Start();
        }



        private void populateComboBox()
        {
            var enumListFunction = Enum.GetNames(typeof(PXCMFaceData.ExpressionsData.FaceExpression));
            cbExprSelect.ItemsSource = enumListFunction;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "(Wave Your Hand)";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //DISPOSE OF OBJECTS PLEASE
            processingThread.Abort();
            handData.Dispose();
            handConfig.Dispose();
            faceConfig.Dispose();
            pipeClient.Stop();
            senseManager.Dispose();
            file.Close();
            
        }

        protected override void OnClosed(EventArgs e)
        {
            //DISPOSE OF OBJECTS PLEASE
            processingThread.Abort();
            handData.Dispose();
            handConfig.Dispose();
            faceConfig.Dispose();
            pipeClient.Stop();
            senseManager.Dispose();
            file.Close();

            
        }

        //private void PipesMessageHandler(string message)
        //{
        //    try
        //    {

        //        if (Convert.ToInt32(message.Trim()) > 0)
        //        {
        //            stimcode = Convert.ToInt32(message.Trim());
        //            Debug.WriteLine("New stimcode: " + stimcode);
        //        }

        //    }
        //    catch (Exception ex)
        //    {

        //        Debug.WriteLine(ex.Message);
        //    }

        //}
        private void OnServerMessage(NamedPipeConnection<string,string> connection, string message)
        {
            Console.WriteLine("Server says: {0}", message);
        }

        private void OnError(Exception exception)
        {
            Console.Error.WriteLine("ERROR: {0}", exception);
        }
        

        private void ProcessingThread()
        {
            // Start AcquireFrame/ReleaseFrame loop - MAIN PROCESSING LOOP
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                if (firstFrame == true)
                {
                    firstFrame = false;
                    //pipeClient.SendMessage(CAMERA_CONNECTED_MESSAGE);
                }

                //Get sample from the sensemanager to convert to bitmap and show
                PXCMCapture.Sample sample = senseManager.QuerySample();
                Bitmap colorBitmap;
                PXCMImage.ImageData colorData = null;
                

                // Get color/ir image data
                if (cameraMode == "Color")
                    sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                else if (cameraMode == "IR")
                    sample.ir.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                else if (cameraMode == "Depth")
                    ;// -> broken! // sample.depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH, out colorData);
                else
                    sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);

                //convert it to bitmap 
                colorBitmap = colorData.ToBitmap(0, sample.color.info.width, sample.color.info.height);

                // Retrieve hand and face data AND EMOTION DATA
                hand = senseManager.QueryHand();
                face = senseManager.QueryFace();
                emotion = senseManager.QueryEmotion();

                //Process hand data 
                if (hand != null)
                {
                    // Retrieve the most recent processed data
                    handData = hand.CreateOutput();
                    handData.Update();
                    handWaving = handData.IsGestureFired("wave", out gestureData);
                }

                //Process face data 
                if (face != null)
                {
                    // Retrieve the most recent processed data
                    faceData = face.CreateOutput();
                    faceData.Update();
                    numFacesDetected = faceData.QueryNumberOfDetectedFaces();
                    if (numFacesDetected > 0)
                    {
                        // for (Int32 i = 0; i < numFacesDetected; i++) --> MULTIPLE FACE DETECTION DISABLED, UNCOMMENT TO INCLUDE
                        // {
                        // PXCMFaceData.Face singleFace = faceData.QueryFaceByIndex(i); --> FOR MULTIPLE FACE DETECTION

                        //get all possible data from frame
                        PXCMFaceData.Face singleFaceData = faceData.QueryFaceByIndex(0); //only getting first face!
                        PXCMFaceData.ExpressionsData singleExprData = singleFaceData.QueryExpressions();
                        PXCMFaceData.DetectionData detectionData = singleFaceData.QueryDetection();
                        PXCMFaceData.LandmarksData landmarksData = singleFaceData.QueryLandmarks();
                        PXCMFaceData.PoseData poseData = singleFaceData.QueryPose();



                        //Work on face location data from detectionData
                        if (detectionData != null)
                        {
                            // vars are defined globally
                            detectionData.QueryBoundingRect(out boundingRect);
                            detectionData.QueryFaceAverageDepth(out averageDepth);
                        }

                        //Work on getting landmark data
                        if (landmarksData != null)
                        {
                            //var is defined globally
                            landmarksData.QueryPoints(out landmarkPoints);
                        }

                        //Work on getting euler angles for face pose data
                        if (poseData != null)
                        {
                            
                            //var is defined globally
                            poseData.QueryPoseAngles(out eulerAngles);
                            poseData.QueryPoseQuaternion(out quaternionAngles);
                            
                           
                            
                        }


                        //Do work on all face location data from singleExprData
                        if (singleExprData != null)
                        {
                            //get scores and intensities for right and left eye closing - 22 possible expressions --> put into hashtable
                            PXCMFaceData.ExpressionsData.FaceExpressionResult score;
                            
                            //this gets a list of enum names as strings
                            var enumNames = Enum.GetNames(typeof(PXCMFaceData.ExpressionsData.FaceExpression));
                            //for all enumnames, calculate the 
                            for (int j = 0; j < enumNames.Length; j++)
                            {
                                PXCMFaceData.ExpressionsData.FaceExpressionResult innerScore;
                                singleExprData.QueryExpression((PXCMFaceData.ExpressionsData.FaceExpression)(j), out innerScore);

                                //Console.WriteLine((PXCMFaceData.ExpressionsData.FaceExpression)(j));
                                exprTable[enumNames[j]] = innerScore.intensity;


                            }

                            //Attempt to write to file if there are any significant events
                            /*   //check if everything is 0
                               bool significantEntry = false;
                               foreach (DictionaryEntry entry in exprTable)
                               {
                                   if (Convert.ToInt32(entry.Value.ToString()) != 0)
                                   {
                                       significantEntry = true;
                                       break;
                                   }

                               }
                               if (significantEntry) */
                            writeSignificantToFile(exprTable, boundingRect, averageDepth, landmarkPoints, eulerAngles, quaternionAngles);

                            singleExprData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT, out score);
                            lEyeClosedIntensity = score.intensity;

                            singleExprData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT, out score);
                            rEyeClosedIntensity = score.intensity;

                           

                            //eye closed logic -> will be reset in UI thread after some number of frames
                            if (lEyeClosedIntensity >= EYE_CLOSED_DETECT_THRESHOLD)
                                lEyeClosed = true;

                            if (rEyeClosedIntensity >= EYE_CLOSED_DETECT_THRESHOLD)
                                rEyeClosed = true;
                        }




                        // }
                    }

                }
                
                if (emotion != null)
                {
                    int numFaces = emotion.QueryNumFaces();
                    for (int fid = 0; fid < numFaces; fid++)
                    {
                        //TODO - MULTIPLE FACE IMPLEMENTATION?
                        //retrieve all est data
                        PXCMEmotion.EmotionData[] arrData = new PXCMEmotion.EmotionData[10];
                        emotion.QueryAllEmotionData(fid, out arrData);

                        //find emotion with maximum evidence
                        int idx_outstanding_emotion = 0;
                        int max_evidence = arrData[0].evidence;
                        for (int k = 1; k < 7; k++)
                        {
                            if (arrData[k].evidence < max_evidence)
                            {

                            }
                            else
                            {
                                max_evidence = arrData[k].evidence;
                                idx_outstanding_emotion = k;
                            }
                            

                        }

                        currentEmotion = arrData[idx_outstanding_emotion].eid;
                        //Console.WriteLine(currentEmotion.ToString());
                        emotionEvidence = max_evidence;

                       // Console.WriteLine(currentEmotion.ToString() + ":" + emotionEvidence.ToString());
                        

                    }
                }
                

                // Update the user interface
                UpdateUI(colorBitmap);
               

                // Release the frame
                if (handData != null) handData.Dispose();
               // colorBitmap.Dispose();
                sample.color.ReleaseAccess(colorData);
                senseManager.ReleaseFrame();

            }
        }

        //private void sendPipeMessage(string pipeName, string message)
        //{
        //    try
        //    {
        //        _pipeClient.Send(message, pipeName);
        //    }
        //    catch (Exception ex)
        //    {

        //        Debug.WriteLine(ex.Message);
        //    }
            
        //}


        private void UpdateUI(Bitmap bitmap)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (bitmap != null)
                {


                    //calculate FPS
                    if (prevTime == 0)
                    {
                        stopwatch.Start();
                        prevTime = stopwatch.ElapsedMilliseconds;
                    }
                    else
                    {
                        double elapsed = stopwatch.ElapsedMilliseconds - prevTime;
                        fps = 1000 / elapsed;
                        lblFPS.Content = "FPS: " + Convert.ToInt32(fps).ToString();
                        prevTime = stopwatch.ElapsedMilliseconds;
                    }


                    // Mirror the color stream Image control
                    imgColorStream.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    ScaleTransform mainTransform = new ScaleTransform();
                    mainTransform.ScaleX = -1;
                    mainTransform.ScaleY = 1;
                    imgColorStream.RenderTransform = mainTransform;

                    // Display the color stream
                    imgColorStream.Source = ConvertBitmap.BitmapToBitmapSource(bitmap);

                    

                    // Update the hand waving label and start reset cycle
                    if (handWaving)
                    {
                        lblMessage.Content = "Hello World!";
                        handTrigger = true;
                    }

                    // Update the lEyeClosed label and start reset cycle
                    if (lEyeClosed)
                    {
                        lblLEyeClosed.Content = "Left Eye Closed: Yes";
                        lEyeClosedTrigger = true;
                    }
                    if (rEyeClosed)
                    {
                        lblREyeClosed.Content = "Right Eye Closed: Yes";
                        rEyeClosedTrigger = true;
                    }
                    if (lEyeClosedTrigger && rEyeClosedTrigger)
                    {

                        blinkTrigger = true;
                        lblBlink.Content = "Blink Detected: Yes";

                        //WRITE TO FILE!
                        //writeBlinkToFile();


                    }

                    //update other labels
                    lblFacesDetected.Content = "Number of Faces Detected: " + numFacesDetected.ToString();
                    lblLEyeClosedIntensity.Content = "Left Eye Closed Intensity: " + lEyeClosedIntensity.ToString();
                    lblREyeClosedIntensity.Content = "Right Eye Closed Intensity: " + rEyeClosedIntensity.ToString();

                    lblAverageDepth.Content = "Average Depth: " + averageDepth.ToString();
                    lblBoundingRect.Content = "Bounding Rect: height: " + boundingRect.h + " width: " + boundingRect.w + " x: " +
                                               boundingRect.x + " y: " + boundingRect.y;
                    //update selected expression label
                    if (exprTable.ContainsKey(exprToDisplay))
                        lblExprIntensity.Content = exprTable[exprToDisplay];

                    //update landmarkpoint label
                    string lpDisplayString = "";
                    if (landmarkPoints != null)
                    {
                        for (int lPoint = 0; lPoint < landmarkPoints.Length; lPoint++)
                            lpDisplayString += "Point: " + lPoint + " - " + "X: " + landmarkPoints[lPoint].image.x + " Y: " + landmarkPoints[lPoint].image.y + Environment.NewLine;

                    }
                    textBlockLandmarkPoints.Text = lpDisplayString;

                    //update euler angles label
                    if (eulerAngles != null)
                        lblPoseEulerAngles.Content = "EULER ANGLES" + Environment.NewLine + "Pitch: " + eulerAngles.pitch + " Roll: " + eulerAngles.roll + " Yaw: " + eulerAngles.yaw;
                    if (quaternionAngles != null)
                        lblPoseQuaternionAngles.Content = "QUATERNION ANGLES" + Environment.NewLine + "W: " + quaternionAngles.w + " X: " + quaternionAngles.x + " Y: " + quaternionAngles.y + " Z: " + quaternionAngles.z;
                  
                    
                    //Process Emotion:
                    lblEmotionEvidence.Content = "Emotion Evidence: " + emotionEvidence;
                    switch (currentEmotion)
                    {

                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_ANGER:
                            lblEmotion.Content = "Emotion: Anger";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_CONTEMPT:
                            lblEmotion.Content = "Emotion: Contempt";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_DISGUST:
                            lblEmotion.Content = "Emotion: Disgust";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_FEAR:
                            lblEmotion.Content = "Emotion: Fear";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_JOY:
                            lblEmotion.Content = "Emotion: Joy";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_SADNESS:
                            lblEmotion.Content = "Emotion: Sadness";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_PRIMARY_SURPRISE:
                            lblEmotion.Content = "Emotion: Surprise";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_SENTIMENT_NEGATIVE :
                            lblEmotion.Content = "Emotion: Negative Sentiment";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_SENTIMENT_NEUTRAL:
                            lblEmotion.Content = "Emotion: Neutral Sentiment";
                            break;
                        case PXCMEmotion.Emotion.EMOTION_SENTIMENT_POSITIVE:
                            lblEmotion.Content = "Emotion: Positive Sentiment";
                            break;
                        default:
                            lblEmotion.Content = "Emotion: ?";
                            break;


                    }
                     


                    // Reset the screen messages after their globally defined no. of frames
                    if (handTrigger)
                    {
                        handResetTimer++;

                        if (handResetTimer >= HAND_TIMER_RESET_FRAMES)
                        {
                            lblMessage.Content = "(Wave Your Hand)";
                            handResetTimer = 0;
                            handTrigger = false;
                        }
                    }

                    if (lEyeClosedTrigger)
                    {
                        lEyeClosedResetTimer++;

                        if (lEyeClosedResetTimer >= EYE_TIMER_RESET_FRAMES)
                        {
                            lblLEyeClosed.Content = "Left Eye Closed: No";
                            lEyeClosedResetTimer = 0;
                            lEyeClosed = false;
                            lEyeClosedTrigger = false;
                        }
                    }

                    if (rEyeClosedTrigger)
                    {
                        rEyeClosedResetTimer++;

                        if (rEyeClosedResetTimer >= EYE_TIMER_RESET_FRAMES)
                        {
                            lblREyeClosed.Content = "Right Eye Closed: No";
                            rEyeClosedResetTimer = 0;
                            rEyeClosed = false;
                            rEyeClosedTrigger = false;
                        }


                    }



                    if (blinkTrigger == true)
                    {
                        blinkResetTimer++;
                        if (blinkResetTimer >= BLINK_TIMER_RESET_FRAMES)
                        {

                            lblBlink.Content = "Blink Detected: No";
                            blinkResetTimer = 0;
                            blinkTrigger = false;
                        }
                    }

                }
            }));
        }

        private void addBlinkToFile() //not in use currently
        {
            file.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //System.Windows.MessageBox("Wrote " + DateTime.Now.ToString("yyyyMMddHHmmss") + " to file");
        }

        private void writeSignificantToFile(Hashtable iHashTable, PXCMRectI32 iBoundingRect, float iAverageDepth, PXCMFaceData.LandmarkPoint[] iLandmarkPoints, PXCMFaceData.PoseEulerAngles iEulerAngles, PXCMFaceData.PoseQuaternion iQuaternionAngles)
        {


            //write the timestamp

            string toWrite = DateTime.Now.ToString("yyyyMMddHHmmssfff"); //First timestamp
            double highPerfElapsed;

            if (numLinesWritten == 0)
            {
                toWrite += ",0"; //high perf timestamp is 0 to indicate first ever line written
                highPerformanceTimer.Start();
            }
            else
            {
                //stop the timer, then get the elapsed time with the Duration internal function
                highPerformanceTimer.Stop();
                highPerfElapsed = highPerformanceTimer.Duration;



                //restart the timer, only missed timings are the time taken to set 1 value
                //Likely to build up error over time
                highPerformanceTimer.Start();

                //write to the string builder
                toWrite += "," + totalHighPerfTimeElapsed.ToString();

                totalHighPerfTimeElapsed += highPerfElapsed;


                //Console.WriteLine(totalHighPerfTimeElapsed);
                //Console.WriteLine(DateTime.Now.Second);

            }


            //write current stim code then reset it
            toWrite += "," + stimcode;
            stimcode = 0;


            //add all the expressiondata
            var enumListFunction = Enum.GetNames(typeof(PXCMFaceData.ExpressionsData.FaceExpression));
            for (int i = 0; i < enumListFunction.Length; i++)
            {
                toWrite += "," + iHashTable[enumListFunction[i]];
            }

            //add the bounding rectangle data
            toWrite += "," + iBoundingRect.h + "," + iBoundingRect.w + "," + iBoundingRect.x + "," + iBoundingRect.y;

            //add average depth data
            toWrite += "," + iAverageDepth;

            // add all landmarkpoints data
            if (iLandmarkPoints != null)
            {
                for (int i = 0; i < iLandmarkPoints.Length; i++)
                {
                    toWrite += "," + iLandmarkPoints[i].image.x + "," + iLandmarkPoints[i].image.y;
                }
            }
            


            //add euler angles
            toWrite += "," + iEulerAngles.pitch + "," + iEulerAngles.roll + "," + iEulerAngles.yaw;

            //add quaternion angles
            toWrite += "," + iQuaternionAngles.w + "," + iQuaternionAngles.x + "," + iQuaternionAngles.y + "," + iQuaternionAngles.z;


            file.WriteLine(toWrite); //actually write the entire string to file
            numLinesWritten++; //increase number of lines written counter




        }

        private void cbExprSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            exprToDisplay = cbExprSelect.SelectedItem.ToString();
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            cameraMode = cbCameraMode.SelectedItem.ToString();
        }

        



    }

    public class MyClient
    {
        /// <summary>
        /// Modified class to act as a named pipe server. Can add own function to subscribe to connection message event etc
        /// </summary>
        private NamedPipeClient<string> client;


        public MyClient(string pipeName)
        {
            this.client = new NamedPipeClient<string>(pipeName);
            client.ServerMessage += OnServerMessage;
            client.Error += OnError;
            client.Start();

        }

        private void OnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            Console.WriteLine("Server says: {0}", message);
            int value;
            if (int.TryParse(message, out value))
            {
                if (value > 0)
                    MainWindow.stimcode = value;
                else
                    throw new ArgumentOutOfRangeException("Stimcode should only be positive");
            }
            
        }

        private void OnError(Exception exception)
        {
            Console.Error.WriteLine("ERROR: {0}", exception);
        }

        public void SendMessage(string message)
        {
            client.PushMessage(message);
        }

        public void Stop()
        {
            client.Stop();
        }
    }

}

