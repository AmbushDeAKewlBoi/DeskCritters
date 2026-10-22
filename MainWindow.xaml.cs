
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DeskCritters;

public partial class MainWindow : Window
{
    private DispatcherTimer movementTimer;
    private double speed = 2;
    private int direction = 1;
    private Random random = new Random();
    private bool isWalking = true;
    private int ticksUntilChange = 100;

    public MainWindow()
    {
        InitializeComponent();

        Left = SystemParameters.WorkArea.Right - Width - 50;
        Top = SystemParameters.WorkArea.Bottom - Height - 50;

        movementTimer = new DispatcherTimer();
        movementTimer.Interval = TimeSpan.FromMilliseconds(30);
        movementTimer.Tick += MoveCritter;
        movementTimer.Start();
    }

    private void MoveCritter(object? sender, EventArgs e){
        ticksUntilChange--;

        if (ticksUntilChange <= 0){
            isWalking = random.Next(3) != 0;

            if (isWalking){
                direction = random.Next(2) == 1 ? 1 : -1;
                ticksUntilChange = random.Next(33,100);

            }else{
                ticksUntilChange = random.Next(10, 34);
            }

        }
        if(!isWalking){
            return;
        }
        Left += speed*direction;

        double rightBoundary = SystemParameters.WorkArea.Right - Width;
        double leftBoundary = SystemParameters.WorkArea.Left;

        if( Left >= rightBoundary){
            Left = rightBoundary;
            direction = -1;
        } else if (Left <= leftBoundary){
            Left = leftBoundary;
            direction =1;
        }
    }

    private void Critter_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            movementTimer.Stop();

            try{
                DragMove();
            }
            finally{
                movementTimer.Start();
            }
        }
    }
}
