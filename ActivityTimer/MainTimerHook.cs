using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace ActivityTimer
{
    class MainTimerHook : Form
    {
        private static LowLevelKeyboardProc _procedure = HCallback;
        private static LowLevelMouseProc _procedure_2 = HCallbackMouse;

        private static IntPtr _hookID = IntPtr.Zero;
        private static IntPtr _hookID_2 = IntPtr.Zero;

        private static int _xflipcalc = 0;
        private static int _runOnce = 0;
        public static int x = 0;
        private static int _currentState = 0;
        private static double _mins = 5;

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WH_LBUTTONDOWN = 0x0201;

        public static bool _flip = true;
        public static bool _flipTimeWatcher = true;
        public static bool _flipTimeWatcher_2 = true;

        private static System.Timers.Timer _timer;
        public static Stopwatch stopWatch = new Stopwatch();
        private static DateTime _startTime;

        private static Publish publish;
        private static TimeTrigger subs;

        public static double _tempRunTime = 0;
        public static double _tempRunTimeCompare = 0;
        public static int? _tempRunTimeSubtraction = 0;

        private static List<double> lsta = new List<double>(2);

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
        private delegate void delegatePointer();

        [STAThread]
        static void Main(string[] args)
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("ActivityTimer 1.0");

            publish = new Publish();
            subs = new TimeTrigger();

            publish.Triggered += subs.OnPeripheralActivity;
            
            _hookID = SetHook(_procedure, null);

            _hookID_2 = SetHook(null, _procedure_2);

            Application.Run();

            ReleaseHook(IntPtr.Zero);


        }

        private static void TimeStarter(double _tempRunTime) //_tempRunTime not used for now
        {

            //_flip = true;
            _xflipcalc++;

            _flipTimeWatcher = false;

            if (_flipTimeWatcher == false)
            {
                stopWatch.Start();
            }


        }
        private void TimeStopper()
        {
            _flip = true;
            _flipTimeWatcher = true;
            

            _xflipcalc++;
            _timer = new System.Timers.Timer(10000);
            _timer.Elapsed += timerInterval;

            Console.WriteLine("Time stopped at" + " " + stopWatch.Elapsed);

            stopWatch.Stop();
        }
        //Not using Task due to target framework restrictions
        private void TimeWatcher() {

            
            _timer = new System.Timers.Timer(10000);
        
            _timer.Elapsed += timerInterval;
            _timer.Enabled = true;
            Console.WriteLine("Timer started" + " " + stopWatch.Elapsed);


        }
        private void timerInterval(object sender, System.Timers.ElapsedEventArgs e)
        {

            Console.WriteLine("TimeI");
            
            if (_timer != null) { 
                _timer.Elapsed -= timerInterval;
                _timer.Enabled = false;
                
            }
            if (_flip == true) {

                Console.WriteLine("True");
                //_flipTimeWatcher_2 = false;


            }
            if (lsta.Count == 2) { 

                if((_flip == false) && (_timer == null) && (lsta[1]-lsta[0]) > 5 && _flipTimeWatcher_2 == false )
                {
                
                    TimeStopper();
                    Console.WriteLine("False");
                }
            }
            //Console.WriteLine(stopWatch.Elapsed);
        }

        //Unable to set messagefilter in any way to console window, console window belongs to the CSRSS service. CSRSS is a critical system service that is protected and cannot be hooked without special debug privileges.
        /*protected override void WndProc(ref Message m)
        {

            if(m.Msg)
            {
                
            }

            base.WndProc(ref m);

        }*/


        private static IntPtr SetHook(LowLevelKeyboardProc procKey, LowLevelMouseProc procMouse)
        {
            while(_runOnce <= 1)
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
            TimeSpan ts = stopWatch.Elapsed;

            try {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) // Pointer WM_KEYDOWN url for documentation windows/desktop/legacy/ms644985(v=vs.85) The identifier of the keyboard message. This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.
                {

                    if (_flip == true) //&& (_timer == null)
                    {
                       
                        Keys key = (Keys)Marshal.ReadInt32(lParam);

                        if (((key >= Keys.A && key <= Keys.Z) | key == Keys.F12 | (key >= Keys.D0 && key <= Keys.D9) | key == Keys.Enter)) //_flip is initialized as true at startup
                        {

                            _tempRunTime = ts.TotalSeconds;
                            _flip = false;
                            Console.WriteLine("Key");
                            CompareTimes();
                            ReleaseHook(_hookID);
                            _hookID = SetHook(_procedure, null);

                        }

                    }
                    else {

                        _timer = null;
                        _tempRunTime = ts.TotalSeconds;
                        CompareTimes();
                        Console.WriteLine(_tempRunTime);
                        //Console.WriteLine(lsta[_xflipcalc-1]);
                        Console.WriteLine(lsta.Count);
 
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
            TimeSpan ts = stopWatch.Elapsed;

            try
            {

                if ((nCode >= 0 && wParam == (IntPtr)WH_LBUTTONDOWN)) //_flip is initialized as true at startup
                {
                    if (_flip == true) // && (_timer == null)
                    {
                        
                        Console.WriteLine("Mouse");
                        _flip = false;

                        ReleaseHook(_hookID_2);
                        _hookID_2 = SetHook(null, _procedure_2);

                    }
                    else {

                        _timer = null;
                        _tempRunTime = ts.TotalSeconds;
                        Console.WriteLine(_tempRunTime);

                    }
                }
                
               
            }
            catch (Exception e)
            {
                
                AppDestroy(e);

            }
            
            return CallNextHookEx(_hookID_2, nCode, wParam, lParam);
        }
        private static void CompareTimes()
        {

            if (lsta.Count == 2)
            {
                Console.WriteLine(lsta[1]-lsta[0]);

                if (lsta[1] - lsta[0] > _mins)
                {

                    Console.WriteLine("Yli viisi");
                    _flipTimeWatcher_2 = false;

                }
                if (lsta[1] - lsta[0] < _mins)
                {
                    lsta.RemoveAt(0);
                    Console.WriteLine("Alle viisi");
                }
            }
            else
            {
                if (lsta.Count <= 1)
                {
                    lsta.Add(_tempRunTime);
                    Console.WriteLine(lsta.Count);
                }
            }
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
            public event EventHandler? checkTime;
            public void OnPeripheralPressed()

            {

                if (Triggered != null)
                {
                    Triggered?.Invoke(this, EventArgs.Empty);
                }
  
            }

        }
        class TimeTrigger : MainTimerHook
        {
            
            public void OnPeripheralActivity(object sender, EventArgs e)

            {
                
                if (x < 1)
                {
                    
                    x = 1;

                }
                else if (x < 2) {

                    x = 2;

                }
                else
                {

                    TimeStarter(_tempRunTime);
                    TimeWatcher();
                }

            }
            

        }
       
    }

    }
