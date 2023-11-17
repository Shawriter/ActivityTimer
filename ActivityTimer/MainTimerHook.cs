using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Timers;

namespace ActivityTimer
{
    class MainTimerHook : Form
    {
        private static LowLevelKeyboardProc _procedure = HCallback;
        private static LowLevelMouseProc _procedure_2 = HCallbackMouse;

        private static IntPtr _hookID = IntPtr.Zero;
        private static IntPtr _hookID_2 = IntPtr.Zero;

        private static int _runOnce = 0;
        public static int x = 0;
        private static double sec = 30.0; //Change this to change how long to be inactive before the timer stops

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WH_LBUTTONDOWN = 0x0201;

        public static bool _flip = true; //Boolean flipswitches to ensure running blocks of code only when needed
        public static bool _flip_2 = true;
        public static bool _flipTimeWatcher = true;
        public static bool _flipTimeWatcher_2 = true;

        public static Stopwatch stopWatch = new Stopwatch();
        public static Stopwatch stopWatch_2 = new Stopwatch();
        public static TimeSpan ts = stopWatch.Elapsed;
        public static TimeSpan ts_2 = stopWatch_2.Elapsed;

        private static Publish publish;
        private static TimeTrigger subs;

        private static double _tempRunTime;
        private static double _tempRunTime_2;
 
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int MessageBoxW(IntPtr hWnd, [param: MarshalAs(UnmanagedType.LPWStr)] string lpText, [param: MarshalAs(UnmanagedType.LPWStr)] string lpCaption, UInt32 uType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        [return: MarshalAs(UnmanagedType.Bool)]

        private static extern bool GetMessage(out IntPtr lpMsg, IntPtr hWnd, uint wMsgMin, uint wMsgMax);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        [STAThread]
        static void Main(string[] args)
        {

            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("ActivityTimer 1.0");
            Console.WriteLine("Press any key to start the timer. " + "\n" +
                "Timer will automatically stop after 30 seconds of not pressing the keyboard or the mouse.");
            Console.ReadKey();

            publish = new Publish();
            subs = new TimeTrigger();
            SetTempRunTime();
            publish.Triggered += subs.OnPeripheralActivity;

            _hookID = SetHook(_procedure, null);

            _hookID_2 = SetHook(null, _procedure_2);

            Application.Run();

            ReleaseHook(IntPtr.Zero);


        }

        private static void TimeStarter() 
        {
            DateTime date = DateTime.Now;
            _flipTimeWatcher = false;

            if (_flipTimeWatcher == false)
            {
                stopWatch.Start();
                stopWatch_2.Start();
                if (_tempRunTime > 0.1)
                { //If not this if clause, the program will print twice at the start 

                    Console.WriteLine("Timer started" + " at " + stopWatch.Elapsed + " Hours/minutes/seconds " + "\n" + date + "\n");
                }
            }



        }
        public static void TimeStopper()
        {
            DateTime date = DateTime.Now;
            _flip = true;
            _flipTimeWatcher = true;
            
            if(_flip_2 == false) { 

                 Console.WriteLine("Timer stopped at" + " " + stopWatch.Elapsed + " Hours/minutes/seconds " + "\n" + date + "\n");
                 _flip_2 = true;
                 stopWatch.Stop();
                 Console.WriteLine("Press any key to start the timer again");
                 Console.ReadKey();
            }

        }
        //Not using Task due to target framework restrictions
        class TimeWatcherClass
        {
            private static System.Timers.Timer _timer;
            public void TimeWatcher()
            {
                if (_timer == null)
                {
                    _timer = new System.Timers.Timer(2000); //Every 2 seconds check the time difference between temptimes
                    _timer.Elapsed += TimerInterval;
                    _timer.AutoReset = true;
                    _timer.Enabled = true;
                }

            }

        }
       
        private static void TimerInterval(object sender, System.Timers.ElapsedEventArgs e)
        {
            
            SetTempRunTime();
            _tempRunTime_2 = stopWatch_2.Elapsed.TotalSeconds;
            CompareTimes();

        }

        private static void CompareTimes()
        {

           
            if(_tempRunTime_2 == 0.0)
            {
                stopWatch_2.Start();
            }
            if (_tempRunTime_2 >= sec && (_tempRunTime_2 != 0.0)) 
            {

                TimeStopper();
                stopWatch_2.Reset();
               
            }
            //Console.WriteLine("Pass");

        }

        private static void SetTempRunTime() {

 
            if (_tempRunTime < 1)
            {

                _tempRunTime = stopWatch.Elapsed.TotalSeconds;
                
            }
            else
            {
                _tempRunTime = stopWatch.Elapsed.TotalSeconds;  
            }

        }
        private static IntPtr SetHook(LowLevelKeyboardProc procKey, LowLevelMouseProc procMouse)
        {
            while (_runOnce <= 1)
            {
                _runOnce = 2;
            }
            if ((_runOnce >= 1))
            {
                publish.OnPeripheralPressed();
            }
            try
            {

                Process currentProcess = Process.GetCurrentProcess();
                ProcessModule currentModule = currentProcess.MainModule;

                if (procKey != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, procKey, GetModuleHandle(currentModule.ModuleName), 0); // 13 = The WH_KEYBOARD_LL hook enables you to monitor keyboard input events about to be posted in a thread input queue.
                }
                if (procMouse != null)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, procMouse, GetModuleHandle(currentModule.ModuleName), 0); // 14 = WH_MOUSE_LL Installs a hook procedure that monitors low - level mouse input events.For more information, see the LowLevelMouseProc hook procedure.
                }
                else
                {
                    Console.WriteLine("Pass");
                }
            }
            catch (Exception e)
            {
                AppDestroy(e);

            }
            return IntPtr.Zero;
        }

        private static IntPtr HCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {


            try {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) // Pointer WM_KEYDOWN url for documentation windows/desktop/legacy/ms644985(v=vs.85) The identifier of the keyboard message. This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.
                {

                    if (_flip == true) 
                    {

                        Keys key = (Keys)Marshal.ReadInt32(lParam);

                        if (((key >= Keys.A && key <= Keys.Z) | key == Keys.F12 | (key >= Keys.D0 && key <= Keys.D9) | key == Keys.Enter)) //_flip is initialized as true at startup
                        {


                            SetTempRunTime();
                            _flip = false;
                            _flip_2 = false;
                            ReleaseHook(_hookID);
                            _hookID = SetHook(_procedure, null);
                            

                        }

                    }
                    else {

                        stopWatch_2.Reset();
                        _flip_2 = false;
                        SetTempRunTime();

                    }
                }

            }
            catch (Exception e)
            {

                AppDestroy(e);

            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);

        }

        public static IntPtr HCallbackMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {


            try
            {

                if ((nCode >= 0 && wParam == (IntPtr)WH_LBUTTONDOWN)) //_flip is initialized as true at startup
                {
                    if (_flip == true) 
                    {
                        SetTempRunTime();
                        _flip = false;
                        _flip_2 = false;
                        ReleaseHook(_hookID_2);
                        _hookID_2 = SetHook(null, _procedure_2);
                        
                    }
                    else {

                        stopWatch_2.Reset();
                        _flip_2 = false;
                        SetTempRunTime();
                        
                    }
                }


            }
            catch (Exception e)
            {

                AppDestroy(e);

            }

            return CallNextHookEx(_hookID_2, nCode, wParam, lParam);
        }
        
       
      
        private static void ReleaseHook(IntPtr hookId)
        {
            UnhookWindowsHookEx(hookId);
        }
        public static void AppDestroy(Exception e)
        {
            MessageBoxW(IntPtr.Zero, "Exiting..." + e.StackTrace, "Error", 0);
            Application.Exit();
        }

        class Publish
        {

            public event EventHandler? Triggered;
            public void OnPeripheralPressed()

            {

                if (Triggered != null)
                {
                    Triggered?.Invoke(this, EventArgs.Empty);
                }
  
            }

        }
        class TimeTrigger
        {

            public void OnPeripheralActivity(object sender, EventArgs e)

            {
                
                TimeStarter();
                TimeWatcherClass timer = new TimeWatcherClass();
                timer.TimeWatcher();
               
            }
            
         

        }
       
    }

  }
