using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;

public class SensorDataReceiver : MonoBehaviour
{
    private TcpListener tcpListener;
    private TcpClient connectedClient;
    private NetworkStream stream;

    // Referencia al componente TextMeshPro para actualizar el texto
    public TextMeshProUGUI data_text;

    // Variables para almacenar los valores de velocidad y rotación angular
    private float VX, VY, VZ, AX, AY, AZ;

    private void Start()
    {
        // Iniciar el servidor
        tcpListener = new TcpListener(IPAddress.Any, 5000);
        tcpListener.Start();
        Debug.Log("Servidor TCP iniciado y escuchando en el puerto 5000.");

        tcpListener.BeginAcceptTcpClient(OnClientConnected, null);
    }

    private void OnClientConnected(IAsyncResult result)
    {
        connectedClient = tcpListener.EndAcceptTcpClient(result);
        stream = connectedClient.GetStream();
        Debug.Log("Cliente conectado.");

        tcpListener.BeginAcceptTcpClient(OnClientConnected, null);

        ReadData();
    }

    private async void ReadData()
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (connectedClient.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Datos recibidos: " + receivedData);

                    // Procesar los datos recibidos
                    ProcessSensorData(receivedData);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al leer los datos del cliente: " + e.Message);
        }
    }
    private void ProcessSensorData(string jsonData)
    {
        try
        {
            // Convertir los datos JSON a un objeto SensorData
            var sensorData = JsonUtility.FromJson<SensorData>(jsonData);
            Debug.Log($"VelocidadX: {sensorData.VelocidadX}, VelocidadY: {sensorData.VelocidadY}, VelocidadZ: {sensorData.VelocidadZ}, AngularX: {sensorData.AngularX}, AngularY: {sensorData.AngularY}, AngularZ: {sensorData.AngularZ}");

            // Almacenar los valores en las variables
            VX = sensorData.VelocidadX;
            VY = sensorData.VelocidadY;
            VZ = sensorData.VelocidadZ;
            AX = sensorData.AngularX;
            AY = sensorData.AngularY;
            AZ = sensorData.AngularZ;

            // Ejecutar la escritura del archivo y la actualización de la UI en el hilo principal
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                WriteDataToFile(); // Ahora se ejecuta en el hilo principal
                UpdateUI();        // También se ejecuta en el hilo principal
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error al procesar los datos: " + e.Message);
        }
    }

    private void WriteDataToFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "SensorData.txt");

        // Limpiar el contenido del archivo antes de escribir nuevos datos
        using (StreamWriter writer = new StreamWriter(filePath, false)) // 'false' para sobreescribir el archivo
        {
            writer.WriteLine($"{VZ:F2}");
            writer.WriteLine($"{VY:F2}");
            writer.WriteLine($"{VX:F2}");
            writer.WriteLine($"{AX:F2}");
            writer.WriteLine($"{AY:F2}");
            writer.WriteLine($"{AZ:F2}");
        }

        Debug.Log($"Datos escritos en el archivo: {filePath}");
    }


    private void UpdateUI()
    {
        if (data_text != null)
        {
            // Ajustar el tamaño de la letra
            data_text.fontSize = 24f; // Ajusta este valor según el tamaño de letra que prefieras

            // Actualizar el texto de la UI con los valores almacenados
            data_text.text = $"VX: {VX:F2}\n" +
                             $"VY: {VY:F2}\n" +
                             $"VZ: {VZ:F2}\n" +
                             $"AX: {AX:F2}\n" +
                             $"AY: {AY:F2}\n" +
                             $"AZ: {AZ:F2}";
        }
        else
        {
            Debug.LogWarning("Referencia a data_text no está asignada.");
        }
    }

    private void OnDestroy()
    {
        stream?.Close();
        connectedClient?.Close();
        tcpListener?.Stop();
    }
}

[Serializable]
public class SensorData
{
    public float VelocidadX;
    public float VelocidadY;
    public float VelocidadZ;
    public float AngularX;
    public float AngularY;
    public float AngularZ;
}
