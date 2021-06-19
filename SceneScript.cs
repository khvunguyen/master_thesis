using UnityEngine;
using UnityEngine.UI;
using OscCore;
using System;
using System.Collections.Generic;

public class SceneScript : MonoBehaviour
{
    // UI elements + OSC receiver
    public InputField inputPort;
    public Text debugInfo;
    public OscReceiver receiver;
    // Saving the previous value of each slider 
    private Dictionary<string, float> valueXYZ;
    // All the address patterns to be defined on the server
    private readonly string addressX = "/x";
    private readonly string addressY = "/y"; 
    private readonly string addressZ = "/z";
    private readonly string addressH = "/hello";
    // Temporary received value is store in these variables
    private float x = 0, y = 0, z = 0;
    private string hello = "";

    // Start is called once when Unity enables the script
    void Start()
    {
        // At the beginning, there's no previous slider value yet (all 0)
        valueXYZ = new Dictionary<string, float>() { { addressX, 0 }, { addressY, 0 }, { addressZ, 0 } };
    }

    // Set listening port for the server (inside the receiver)
    public void SetPortForListening()
    {
        // Parse the input value for port and check if it's "valid" (between 1 and 65535)
        int port = Int32.Parse(inputPort.text);
        if (port >= 1 && port <= 65535)
        {
            // Set the new port to the receiver
            receiver.Port = port;

            if (receiver != null)
            {
                // ReadValue function is called on the receiver (server) background thread
                // After it has finished, MainThreadMethod is called (to update the next frame)                  
                receiver.Server.TryAddMethodPair(addressX, ReadValueX, MainThreadMethodX);
                receiver.Server.TryAddMethodPair(addressY, ReadValueY, MainThreadMethodY);
                receiver.Server.TryAddMethodPair(addressZ, ReadValueZ, MainThreadMethodZ);
                receiver.Server.TryAddMethodPair(addressH, ReadValueH, MainThreadMethodH);

                debugInfo.text = "Server is listening on port " + port;
            }
        }
        else
            debugInfo.text = "Invalid port (must be between 1 and 65535)";
    }

    void ReadValueX(OscMessageValues values)
    {
        // Assign the first element in the message queue at address /x to variable x
        if (values.ElementCount > 0)
            x = values.ReadFloatElement(0);
    }

    void MainThreadMethodX()
    {
        // Update x-position for all children (the cubes inside the parent GameObject)
        foreach (Transform child in transform)
            child.position = new Vector3(child.position.x + (x - valueXYZ[addressX]) * 0.1f,
                                child.position.y, child.position.z);

        // Save the value for previous value of slider X to use next time
        valueXYZ[addressX] = x;
        // Show info on the control board
        debugInfo.text = "Message '" + x + "' was received at " + addressX
        + " on port " + receiver.Port;
    }

    void ReadValueY(OscMessageValues values)
    {
        // Assign the first element in the message queue at address /y to variable y
        if (values.ElementCount > 0)
            y = values.ReadFloatElement(0);
    }

    void MainThreadMethodY()
    {
        // Update y-position for all children (the cubes inside the parent GameObject)
        foreach (Transform child in transform)
            child.position = new Vector3(child.position.x, child.position.y + (y - valueXYZ[addressY]) * 0.1f, child.position.z);

        // Update the value for previous value of slider Y in the dictionary to use next time
        valueXYZ[addressY] = y;
        // Show info on the control board
        debugInfo.text = "Message '" + y + "' was received at " + addressY + " on port " + receiver.Port;
    }

    void ReadValueZ(OscMessageValues values)
    {
        // Assign the first element in the message queue at address /z to variable z
        if (values.ElementCount > 0)
            z = values.ReadFloatElement(0);
    }

    void MainThreadMethodZ()
    {
        // Update z-position for all children (the cubes inside the parent GameObject)
        foreach (Transform child in transform)
            child.position = new Vector3(child.position.x, child.position.y, child.position.z + (z - valueXYZ[addressZ]) * 0.1f);

        // Update the value for previous value of slider Z in the dictionary to use next time
        valueXYZ[addressZ] = z;
        // Show info on the control board
        debugInfo.text = "Message '" + z + "' was received at " + addressZ + " on port " + receiver.Port;
    }

    void ReadValueH(OscMessageValues values)
    {
        // Assign the first element in the message queue at address /hello to variable hello
        if (values.ElementCount > 0)
            hello = values.ReadStringElement(0);
    }

    void MainThreadMethodH()
    {
        // Show info on the control board
        debugInfo.text = "Message '" + hello + "' was received at " + addressH + " on port " + receiver.Port;
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}