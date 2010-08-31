﻿Public Class MidiDevice
    Public Index As Integer 'Index in the list.

    Public WithEvents OutDevice As OutputDevice
    Public WithEvents InDevice As InputDevice


    Public OutDeviceID As Integer = -1
    Public OutChannel As Integer = -1
    Public InDeviceID As Integer = -1
    Public InChannel As Integer = -1

    Public SetVolumeMax As Boolean = True
    Public RemoveOldNotes As Boolean = True
    Public NoteOut As Boolean = True
    Public SendOtherChannels As Boolean = False

    Public Recording As Boolean = False

    Public Sub New(ByVal OutputChannel As Integer, ByVal InputChannel As Integer)
        InChannel = InputChannel
        OutChannel = OutputChannel
    End Sub
    Public Sub New(ByVal InputDevice As InputDevice, ByVal OutputDevice As OutputDevice, ByVal OutputChannel As Integer, ByVal InputChannel As Integer)
        InDevice = InputDevice
        OutDevice = OutputDevice

        InDeviceID = InputDevice.DeviceID
        OutDeviceID = OutputDevice.DeviceID

        InChannel = InputChannel
        OutChannel = OutputChannel
    End Sub


    Public Overrides Function ToString() As String
        Return InputDevice.GetDeviceCapabilities(InDeviceID).name & vbTab & vbTab & OutputDevice.GetDeviceCapabilities(OutDeviceID).name
    End Function

    Public Sub Dispose()
            Recording = False

        If InDevice IsNot Nothing Then
            InDevice.Close()
            InDevice = Nothing
        End If
        If OutDevice IsNot Nothing Then
            OutDevice.Close()
            OutDevice = Nothing
        End If
    End Sub


    Public Sub StartRecording()
        If Recording Then Return

        'Are others recording?
        If InDevice.OthersUsing > 0 Then
            InDevice.OthersUsing += 1

        Else 'lets start recording.
            InDevice.StartRecording()
            InDevice.OthersUsing = 1
        End If

        Recording = True
    End Sub
    Public Sub StopRecording()
        If Not Recording Then Return

        'Are we the only ones recording?
        If InDevice.OthersUsing < 2 Then
            InDevice.StopRecording() 'We are so lets stop it.

        Else 'Lets tell them we are done using.
            InDevice.OthersUsing -= 1
        End If

        ResetNotes()
        SustainList.Clear()
        SostenutoList.Clear()

        Recording = False
    End Sub



    ''' <summary>
    ''' Will build and send a message.
    ''' </summary>
    ''' <param name="message">message to send</param>
    Public Sub Send(ByVal message As Midi.ChannelMessageBuilder)
        If Not Recording Then Return 'If we are not recording then leave.
        message.MidiChannel = OutChannel
        message.Build()
        Send(message.Result)
    End Sub
    Public Sub Send(ByVal message As Midi.ChannelMessage)
        If Not Recording Then Return 'If we are not recording then leave.
        OutDevice.Send(message)

        If Debug Then
            frmMain.AddMessageToDebug(message)
        End If
    End Sub

#Region "Input/Output device events"
    Private Sub InDevice_ChannelMessageReceived(ByVal sender As Object, ByVal e As Sanford.Multimedia.Midi.ChannelMessageEventArgs) Handles InDevice.ChannelMessageReceived
        If Not Recording Then Return 'If we are not recording then leave.

        'If note output is false then leave
        If Not NoteOut Then Return

        'If the message is comeing from the selected midi channel.
        If e.Message.MidiChannel = InChannel Then

            'Create a message from the input device.
            Dim Message As New Midi.ChannelMessageBuilder(e.Message)

            'Is it a note on or off?
            If Message.Command = ChannelCommand.NoteOn Or Message.Command = ChannelCommand.NoteOff Then

                'Is the note on? (volume more then 0)
                If Message.Data2 > 0 Then

                    If SetVolumeMax Then 'Set the volume to max?
                        Message.Data2 = 127 '127 is the maximum Data2 can be. (0 is minimum)
                    End If

                    'If alter note for left pedal is checked then  if the pedal is down then lower the volume.
                    If SoftPressed Then
                        Message.Data2 = 40
                    End If


                    'Pedals
                    If Note(Message.Data1) = Notes.Sostenuto Then
                        If Not SostenutoPressed Then
                            Note(Message.Data1) = Notes.Pressed

                        ElseIf RemoveOldNotes Then
                            ReleaseNote(Message.Data1)
                            Note(Message.Data1) = Notes.Sostenuto
                        End If

                    ElseIf SustainPressed Then
                        If Note(Message.Data1) = Notes.SustainReleased And RemoveOldNotes Then
                            ReleaseNote(Message.Data1)
                        Else
                            SustainList.Add(Message.Data1)
                        End If
                        Note(Message.Data1) = Notes.SustainPressed

                    Else
                        Note(Message.Data1) = Notes.Pressed
                    End If

                Else
                    Select Case Note(Message.Data1)
                        Case Notes.Sostenuto
                            If SostenutoPressed Then
                                Return
                            Else
                                Note(Message.Data1) = Notes.Released
                            End If

                        Case Notes.SustainPressed
                            If SustainPressed Then
                                Note(Message.Data1) = Notes.SustainReleased
                                Return
                            Else
                                Note(Message.Data1) = Notes.Released
                            End If
                        Case Notes.SustainReleased
                            If SustainPressed Then
                                Return
                            Else
                                Note(Message.Data1) = Notes.Released
                            End If

                        Case Notes.Pressed
                            Note(Message.Data1) = Notes.Released
                            Message.Command = ChannelCommand.NoteOff

                    End Select
                End If


                Send(Message)
            End If

        ElseIf SendOtherChannels Then
            Send(e.Message)
        End If

    End Sub

    Private Sub InDevice_SysCommonMessageReceived(ByVal sender As Object, ByVal e As Sanford.Multimedia.Midi.SysCommonMessageEventArgs) Handles InDevice.SysCommonMessageReceived
        If Not Recording Then Return 'If we are not recording then leave
        OutDevice.Send(e.Message)
    End Sub
    Private Sub InDevice_SysExMessageReceived(ByVal sender As Object, ByVal e As Sanford.Multimedia.Midi.SysExMessageEventArgs) Handles InDevice.SysExMessageReceived
        If Not Recording Then Return 'If we are not recording then leave.

        OutDevice.Send(e.Message)
    End Sub
    Private Sub InDevice_SysRealtimeMessageReceived(ByVal sender As Object, ByVal e As Sanford.Multimedia.Midi.SysRealtimeMessageEventArgs) Handles InDevice.SysRealtimeMessageReceived
        If Not Recording Then Return 'If we are not recording then leave.

        OutDevice.Send(e.Message)
    End Sub

#End Region

#Region "Note holder"
    Public Note(127) As Integer

    Public Sub ResetNotes()
        Parallel.ForEach(Note, Sub(n)
                                   n = 0
                               End Sub)
    End Sub

    Public Sub ReleaseNote(ByVal ID As Integer)
        Dim tmp As New ChannelMessageBuilder
        tmp.Command = Midi.ChannelCommand.NoteOn
        tmp.Data1 = ID
        tmp.Data2 = 0
        Note(ID) = Notes.Released
        Send(tmp)
    End Sub
#End Region

#Region "Input"
    Public Input As New List(Of InputData)

    Public Sub DoInput()
        If Not Recording Then Return 'If we are not recording then leave.

        For Each inp As InputData In Input
            inp.DoInput()
        Next

    End Sub

    Public SostenutoList As New List(Of Integer)
    Public SustainList As New List(Of Integer)
    Public SustainPressed, SostenutoPressed, SoftPressed As Boolean
    Friend Sub PressSustain()
        'Check for down keys and set them to sustain.
        Parallel.For(0, CurrentDevice.Note.Length - 1, Sub(n)
                                                           'For n As Integer = 0 To CurrentDevice.Note.Length - 1
                                                           If CurrentDevice.Note(n) = Notes.Pressed Then
                                                               CurrentDevice.Note(n) = Notes.SustainPressed
                                                               SustainList.Add(n)
                                                           End If
                                                           'Next
                                                       End Sub)
    End Sub
    Friend Sub ReleaseSustain()
        Dim tmp As New ChannelMessageBuilder
        tmp.Command = Midi.ChannelCommand.NoteOff
        tmp.Data2 = 0
        For Each n As Integer In SustainList
            If CurrentDevice.Note(n) = Notes.SustainReleased Then
                CurrentDevice.Note(n) = Notes.Released
                tmp.Data1 = n
                CurrentDevice.Send(tmp)
            End If
        Next
        SustainList.Clear()
    End Sub

    Friend Sub PressSostenuto()
        Parallel.For(0, CurrentDevice.Note.Length - 1, Sub(n As Integer)
                                                           If CurrentDevice.Note(n) = Notes.Pressed Or CurrentDevice.Note(n) = Notes.SustainPressed Then
                                                               CurrentDevice.Note(n) = Notes.Sostenuto
                                                               SostenutoList.Add(n)
                                                           End If
                                                       End Sub)
    End Sub
    Friend Sub ReleaseSostenuto()
        Dim tmp As New ChannelMessageBuilder
        tmp.Command = Midi.ChannelCommand.NoteOff
        tmp.MidiChannel = 0
        tmp.Data2 = 0
        For Each n As Integer In SostenutoList
            CurrentDevice.Note(n) = Notes.Released
            tmp.Data1 = n
            CurrentDevice.Send(tmp)
        Next
        SostenutoList.Clear()
    End Sub

#End Region

End Class