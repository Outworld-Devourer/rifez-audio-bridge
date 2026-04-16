package com.rifez.phoneaudio.stream

import android.util.Log
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.cancel
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.io.BufferedReader
import java.io.BufferedWriter
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.ServerSocket
import java.net.Socket
import java.net.SocketException

class ReceiverTcpServer {

    private var serverSocket: ServerSocket? = null
    private var clientSocket: Socket? = null
    private var serverJob: Job? = null
    private var scope: CoroutineScope? = null

    fun start(
        port: Int,
        onStateChanged: (ReceiverSocketState) -> Unit,
        onMessageReceived: (String) -> String
    ) {
        Log.d(TAG, "start() called with port=$port")
        stop()

        try {
            scope = CoroutineScope(Dispatchers.IO)
            serverJob = scope?.launch {
                try {
                    Log.d(TAG, "Creating ServerSocket on port=$port")
                    serverSocket = ServerSocket(port)

                    Log.d(TAG, "TCP server listening on port $port")
                    onStateChanged(ReceiverSocketState.Listening(port))

                    while (isActive) {
                        Log.d(TAG, "Waiting for client...")
                        val socket = serverSocket?.accept() ?: break

                        clientSocket?.close()
                        clientSocket = socket

                        val remoteHost = socket.inetAddress?.hostAddress ?: "unknown"
                        val remotePort = socket.port

                        Log.d(TAG, "Client connected from $remoteHost:$remotePort")
                        onStateChanged(
                            ReceiverSocketState.ClientConnected(
                                port = port,
                                remoteHost = remoteHost,
                                remotePort = remotePort
                            )
                        )

                        handleClient(socket, onMessageReceived)
                    }
                } catch (e: SocketException) {
                    Log.d(TAG, "TCP server stopped: ${e.message}")
                } catch (e: Exception) {
                    Log.e(TAG, "TCP server failed", e)
                    onStateChanged(
                        ReceiverSocketState.Failed(
                            e.message ?: "Unknown socket error"
                        )
                    )
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Failed before coroutine launch", e)
            onStateChanged(
                ReceiverSocketState.Failed(
                    e.message ?: "Failed to start socket server"
                )
            )
        }
    }

    fun stop() {
        try {
            clientSocket?.close()
        } catch (_: Exception) {
        } finally {
            clientSocket = null
        }

        try {
            serverSocket?.close()
        } catch (_: Exception) {
        } finally {
            serverSocket = null
        }

        serverJob?.cancel()
        serverJob = null

        scope?.cancel()
        scope = null
    }

    private fun handleClient(
        socket: Socket,
        onMessageReceived: (String) -> String
    ) {
        try {
            val reader = BufferedReader(InputStreamReader(socket.getInputStream()))
            val writer = BufferedWriter(OutputStreamWriter(socket.getOutputStream()))

            while (true) {
                val line = reader.readLine() ?: break
                Log.d(TAG, "Received: $line")

                val response = onMessageReceived(line)
                writer.write(response)
                writer.newLine()
                writer.flush()

                Log.d(TAG, "Sent: $response")
            }
        } catch (e: SocketException) {
            Log.d(TAG, "Client disconnected: ${e.message}")
        } catch (e: Exception) {
            Log.e(TAG, "Client handling failed", e)
        } finally {
            try {
                socket.close()
            } catch (_: Exception) {
            }
            clientSocket = null
        }
    }

    companion object {
        private const val TAG = "ReceiverTcpServer"
    }
}