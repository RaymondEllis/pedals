﻿Module modLoad

    Public Sub LoadSettings(ByVal File As String)
        Dim sd As New SimpleD.SimpleD(File, True)

        Dim g As SimpleD.Group = sd.Get_Group("Pedals")
        frmMain.chkDebug.Checked = g.Get_Value("Debug")
        frmMain.chkRemoveOldNotes.Checked = g.Get_Value("RemoveOldNotes")

        Dim MidiIn As Integer = g.Get_Value("MidiInput")
        Dim MidiOut As Integer = g.Get_Value("MidiOutput")
        Dim InpCount As Integer = g.Get_Value("Input")


        For n As Integer = 0 To MidiIn
            g = sd.Get_Group("MidiInput_" & n)
            AddInputDevice(g.Get_Value("ID"), g.Get_Value("Channel"))

            g.Get_Value("AllChannels", CurrentMidiInput.AllChannels)
            g.Get_Value("EnableNotes", CurrentMidiInput.EnableNotes)
            g.Get_Value("OtherChannels", CurrentMidiInput.SendOtherChannels)
            g.Get_Value("SetVolumeMax", CurrentMidiInput.SetVolumeMax)

        Next

        For n As Integer = 0 To MidiOut
            g = sd.Get_Group("MidiOutput_" & n)
            AddOutputDevice(g.Get_Value("ID"), g.Get_Value("Channel"))
        Next

        For n As Integer = 0 To InpCount
            g = sd.Get_Group("Input_" & n)
            Dim inp As New InputData(g.Get_Value("ID"), g.Get_Value("Axis"), g.Get_Value("Reverse"), g.Get_Value("SwitchOn"))

            g.Get_Value("Controller", inp.Controller)
            g.Get_Value("controllerType", inp.ControllerType)
            g.Get_Value("AutoName", inp.AutoName)
            g.Get_Value("IsControllerSwitch", inp.IsControllerSwitch)
            g.Get_Value("SwitchOn", inp.SwitchOn)
            g.Get_Value("Name", inp._Name)

            AddInput(inp)
        Next

    End Sub

End Module