﻿Module Input
	Public Joystick As New List(Of Joystick)
	Public dInput As DirectInput
	Public EnableJoysticks As Boolean = False

    Public Enum InputDevices
        Joystick
        Keyboard
        MIDI
    End Enum
    Public Enum ControllerType0
        MIDI
        AlterSustain
        AlterSostenuto
        AlterSoft
    End Enum

	Public Sub CreateInput()
		Status("Creating joystick input...")
		'Create direct input
		dInput = New DirectInput


		'Search for the joysticks.
		For Each Device As DeviceInstance In dInput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly)

			'Try to create the device.
			Try
				Dim joy As New Joystick(dInput, Device.InstanceGuid)
				joy.SetCooperativeLevel(frmMain, CooperativeLevel.Exclusive + CooperativeLevel.Background)
				Joystick.Add(joy)
			Catch ex As Exception
				Status(ex.Message, True)
			End Try
		Next
		If Joystick.Count = 0 Then
			Status("No attached joysticks." & vbNewLine & "Reload the program after you have pluged one in.", True)
			'frmMain.ContinueLoading = False
			'Close()
		Else
			For Each joy As Joystick In Joystick
				joy.Acquire()

				For Each deviceObject As DeviceObjectInstance In joy.GetObjects()
					If (deviceObject.ObjectType And ObjectDeviceType.Axis) <> 0 Then
						joy.GetObjectPropertiesById(CInt(deviceObject.ObjectType)).SetRange(0, 127)
					End If
				Next
			Next
			EnableJoysticks = True
			Status("Creating joystick input... Done!")
		End If



	End Sub

	Public Function GetAxis(ByVal JoysitckID As Integer, ByVal Axis As Integer) As Integer
        If Not EnableJoysticks Then Return 0

		Dim State As JoystickState = Joystick(JoysitckID).GetCurrentState
		Select Case Axis
			Case 0
				Return State.X
			Case 1
				Return State.Y
			Case 2
				Return State.Z

			Case 3
				Return State.RotationX
			Case 4
				Return State.RotationY
			Case 5
				Return State.RotationZ

			Case 6
				Return State.GetSliders(0)
			Case 7
				Return State.GetSliders(1)
		End Select

		Return 0
	End Function

	Public Sub DistroyInput()

		Status("Distroying joystick input...")
		If Joystick.Count > 0 Then
			For Each j As Joystick In Joystick
				If j IsNot Nothing Then
					j.Unacquire()
					j.Dispose()
				End If
			Next
		End If

		If dInput IsNot Nothing Then
            dInput.Dispose()
		End If

		Status("Distroying joystick input... Done!")
	End Sub



End Module


Public Class InputData
    Public Device As MidiDevice
    Public Input As Integer = InputDevices.Joystick

    Public Axis As Integer = -1
    Public ID As Integer = -1
    Public OldPosition As Integer

    Public Reverse As Boolean = True


    ''' <summary>
    ''' If an axis is less then or equals to SwitchOn then turn the switch on.
    ''' </summary>
    Public SwitchOn As Integer

    Public Pressed, Changed As Boolean

    'Output
    Public Controller, ControllerType As Integer
    Public IsControllerSwitch As Boolean = False


    Public Sub New()
    End Sub
    Public Sub New(ByVal Data As String)
        FromString(Data)
    End Sub
    Public Sub New(ByVal ID_ As Integer, ByVal Axis_ As Integer, Optional ByVal Reverse_ As Boolean = True, Optional ByVal SwitchOn_ As Integer = 127)
        ID = ID
        Axis = Axis_
        Reverse = Reverse_
        SwitchOn = SwitchOn_
    End Sub

    Public Sub SetData(ByVal ID_ As Integer, ByVal Axis_ As Integer, Optional ByVal Reverse_ As Boolean = True, Optional ByVal SwitchOn_ As Integer = 127)
        ID = ID
        Axis = Axis_
        Reverse = Reverse_
        SwitchOn = SwitchOn_
    End Sub

    Public Function GetAxisPosition() As Integer
        Dim pos As Integer = 0

        Select Case Input
            Case InputDevices.Joystick
                pos = GetAxis(ID, Axis)


        End Select



        If Reverse Then
            Return (-pos) + 127
        Else
            Return pos
        End If
    End Function

    Public Overrides Function ToString() As String
        Return CType(Input, InputDevices).ToString & "#" & ID & " Axis:" & Axis
    End Function

    Public Sub FromString(ByVal Data As String)
        Dim tmp() As String = Split(Data, ",")
        ID = tmp(0)
        Axis = tmp(1)
        Reverse = tmp(2)
        SwitchOn = tmp(3)
    End Sub


    Public Sub DoInput()
        If Not EnableJoysticks Then Return
        If ID = -1 Then Return
        Dim Pos As Integer = GetAxisPosition()

        'Is the current position the same as the old postion?
        If Pos = OldPosition Then Return 'If so then we need to leave.

        If Pos >= SwitchOn Then
            If Not Pressed Then
                Pressed = True
                Changed = True
            Else
                Changed = False
            End If
        Else
            If Pressed Then
                Pressed = False
                Changed = True
            Else
                Changed = False
            End If
        End If

        'What should we control?
        Select Case ControllerType
            Case ControllerType0.MIDI
                Dim m As New ChannelMessageBuilder
                m.Command = ChannelCommand.Controller
                m.Data1 = Controller
                If IsControllerSwitch And Changed Then
                    If Pressed Then
                        m.Data2 = 127
                    Else
                        m.Data2 = 0
                    End If
                    CurrentDevice.Send(m)
                ElseIf Not IsControllerSwitch Then
                    m.Data2 = Pos
                    CurrentDevice.Send(m)
                End If

            Case ControllerType0.AlterSustain
                If Changed Then
                    If Pressed Then
                        Device.PressSustain()
                    Else
                        Device.ReleaseSustain()
                    End If
                End If
                Device.SustainPressed = Pressed

            Case ControllerType0.AlterSostenuto
                If Changed Then
                    If Pressed Then
                        Device.PressSostenuto()
                    Else
                        Device.ReleaseSostenuto()
                    End If
                End If
                Device.SostenutoPressed = Pressed

            Case ControllerType0.AlterSoft
                Device.SoftPressed = Pressed

        End Select


    End Sub
End Class
