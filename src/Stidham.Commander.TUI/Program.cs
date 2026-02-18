using Terminal.Gui;

// Initialize the application
Application.Init();

// Create the main window
var win = new Window () {
    Title = "Stidham Commander (v1.0)",
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// Add a simple label to verify it works
win.Add(new Label {
    Text = "Welcome to Stidham Commander. Press Ctrl+Q to quit.",
    X = Pos.Center(),
    Y = Pos.Center()
});

// Run the application
Application.Run(win);

// Clean up on exit
Application.Shutdown();
