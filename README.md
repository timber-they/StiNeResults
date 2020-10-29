# StiNeResults
Automatically notifies of new StiNe results.

# How to use
This is a .NET Core 3.1 application and can be build with the corresponding SDK.
You need Firefox installed and need to put the _geckodriver.exe_ next to the built executable of this program.

## Configuration
You can provide a _config_ file (called _config_), in order to avoid having to enter your credentials every time.
In this config, the first line is your username and the second your password (yes, in cleartext). Example:

    baw1234
    aVerySecurePassword!?
