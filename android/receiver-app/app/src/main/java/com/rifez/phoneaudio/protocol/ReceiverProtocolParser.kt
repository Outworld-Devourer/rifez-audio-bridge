package com.rifez.phoneaudio.protocol

object ReceiverProtocolParser {

    fun parse(line: String): ReceiverCommand {
        val trimmed = line.trim()

        if (trimmed.isEmpty()) {
            return ReceiverCommand.Unknown(line)
        }

        val parts = trimmed.split("\\s+".toRegex(), limit = 2)
        val command = parts[0].uppercase()

        return when (command) {
            "HELLO" -> {
                val clientName = parts.getOrNull(1)?.takeIf { it.isNotBlank() }
                ReceiverCommand.Hello(clientName)
            }

            "PING" -> ReceiverCommand.Ping
            "GET_STATUS" -> ReceiverCommand.GetStatus
            "STREAM_START" -> ReceiverCommand.StreamStart
            "DISCONNECT" -> ReceiverCommand.Disconnect
            else -> ReceiverCommand.Unknown(line)
        }
    }
}