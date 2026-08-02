' Bouncing Ball with Sound
' 
' MIT License
' Copyright (c) 2026 Joseph W. Lumbley
' 
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:

' The above copyright notice and this permission notice shall be included in all
' copies or substantial portions of the Software.

' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
' SOFTWARE.

Imports System.Drawing.Drawing2D
Imports System.IO

Public Class Form1

    ' -------------------------------
    '  Engine State
    ' -------------------------------
    Private ballPos As PointF
    'Private ballDiameter As Integer = 80
    Private ballDiameter As Integer = 60


    Private velX As Double
    Private velY As Double
    Private speed As Double = 450

    Private physicsTimer As New Timer()
    Private sw As New Stopwatch()

    ' -------------------------------
    '  FPS Tracking
    ' -------------------------------
    Private frameCount As Integer = 0
    Private fps As Integer = 0
    Private fpsTimer As New Stopwatch()

    ' -------------------------------
    '  GDI Resources
    ' -------------------------------
    Private ballBrush As SolidBrush
    Private fpsBrush As SolidBrush
    Private fpsFont As Font
    Private trailBrushes As SolidBrush()

    ' -------------------------------
    '  Trail System
    ' -------------------------------
    Private trail As New List(Of PointF)
    Private trailLength As Integer = 25
    Private trailSizes As Integer()
    Private trailOffsets As Single()

    Private lastPlay As New Dictionary(Of String, Double)

    Private trailAlpha As Integer()



    Public Sub New()
        InitializeComponent()

        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer, True)

        Me.DoubleBuffered = True
        Me.BackColor = Color.Black
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.WindowState = FormWindowState.Maximized

        ' Center ball
        ballPos = New PointF((ClientSize.Width - ballDiameter) / 2,
                            (ClientSize.Height - ballDiameter) / 2)

        ' Random direction
        Dim rnd As New Random()
        Dim angle As Double = rnd.NextDouble() * Math.PI * 2
        velX = Math.Cos(angle) * speed
        velY = Math.Sin(angle) * speed

        ' Physics at ~60 FPS
        physicsTimer.Interval = 15
        AddHandler physicsTimer.Tick, AddressOf PhysicsTick

        sw.Start()
        fpsTimer.Start()

    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        InitAudio()
        InitGraphics()
        InitTrails()
        InitPhysics()

    End Sub

    Private Sub InitPhysics()

        physicsTimer.Start()

    End Sub

    Private Sub InitGraphics()

        ' Core GDI resources
        ballBrush = New SolidBrush(Color.DeepSkyBlue)
        fpsBrush = New SolidBrush(Color.White)
        fpsFont = New Font("Segoe UI", 14, FontStyle.Bold)

    End Sub

    Private Sub InitTrails()

        '' Preallocate trail brushes
        'trailBrushes = New SolidBrush(trailLength - 1) {}
        'For i As Integer = 0 To trailLength - 1
        '    trailBrushes(i) = New SolidBrush(Color.FromArgb(0, 0, 191, 255))
        'Next

        ' Precompute trail sizes and offsets
        trailSizes = New Integer(trailLength - 1) {}
        trailOffsets = New Single(trailLength - 1) {}

        For i As Integer = 0 To trailLength - 1
            Dim size As Integer = ballDiameter - (trailLength - i) * 2
            If size < 10 Then size = 10

            trailSizes(i) = size
            trailOffsets(i) = CSng((ballDiameter - size) / 2)
        Next

        ' Precompute trail alpha values for exponential fade
        trailAlpha = New Integer(trailLength - 1) {}
        For i As Integer = 0 To trailLength - 1
            Dim t As Double = i / trailLength
            trailAlpha(i) = CInt(32 * t * t)   ' your exponential fade
        Next

        ' Preallocate trail brushes
        trailBrushes = New SolidBrush(trailLength - 1) {}
        For i As Integer = 0 To trailLength - 1
            trailBrushes(i) = New SolidBrush(Color.FromArgb(trailAlpha(i), 0, 191, 255))
        Next

    End Sub

    Private Sub InitAudio()

        CreateSoundFiles()

        'Dim FilePath As String = Path.Combine(Application.StartupPath, "loop.mp3")

        AudioPlayer.AddSound("loop", Path.Combine(Application.StartupPath, "loop.mp3"))

        AudioPlayer.SetVolume("loop", 200)

        AudioPlayer.LoopSound("loop")

        AudioPlayer.AddOverlapping("bounce", Path.Combine(Application.StartupPath, "bounce.mp3"))

        AudioPlayer.SetVolumeOverlapping("bounce", 150)

    End Sub

    ' -------------------------------
    '  Physics Loop (Fixed Timestep)
    ' -------------------------------
    Private Sub PhysicsTick(sender As Object, e As EventArgs)

        Dim dt As Double = sw.Elapsed.TotalSeconds
        sw.Restart()

        dt = Math.Min(dt, 0.05)

        ballPos.X += CSng(velX * dt)
        ballPos.Y += CSng(velY * dt)

        HandleCollisions()
        UpdateTrail()

        Invalidate()
    End Sub

    Private Sub HandleCollisions()

        ' Horizontal bounce
        If ballPos.X <= 0 Then
            ballPos.X = 0
            velX = Math.Abs(velX)

            PlayWithCooldown("bounce", 100)

        ElseIf ballPos.X >= ClientSize.Width - ballDiameter Then
            ballPos.X = ClientSize.Width - ballDiameter
            velX = -Math.Abs(velX)

            PlayWithCooldown("bounce", 100)

        End If

        ' Vertical bounce
        If ballPos.Y <= 0 Then
            ballPos.Y = 0
            velY = Math.Abs(velY)

            PlayWithCooldown("bounce", 100)


        ElseIf ballPos.Y >= ClientSize.Height - ballDiameter Then
            ballPos.Y = ClientSize.Height - ballDiameter
            velY = -Math.Abs(velY)

            PlayWithCooldown("bounce", 100)

        End If

    End Sub

    Public Sub PlayWithCooldown(name As String, ms As Integer)
        Dim now = Environment.TickCount
        If lastPlay.ContainsKey(name) AndAlso now - lastPlay(name) < ms Then
            Return
        End If
        lastPlay(name) = now
        AudioPlayer.PlayOverlapping(name)
    End Sub


    ' -------------------------------
    '  Trail Update
    ' -------------------------------
    Private Sub UpdateTrail()
        trail.Add(New PointF(ballPos.X, ballPos.Y))

        If trail.Count > trailLength Then
            trail.RemoveAt(0)
        End If
    End Sub

    ' -------------------------------
    '  Rendering
    ' -------------------------------
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim g = e.Graphics
        g.CompositingMode = CompositingMode.SourceOver
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.PixelOffsetMode = PixelOffsetMode.HighQuality
        g.InterpolationMode = InterpolationMode.HighQualityBicubic

        DrawTrail(g)
        DrawBall(g)
        DrawFPS(g)

    End Sub

    'Private Sub DrawTrail(g As Graphics)

    '    Dim count As Integer = Math.Min(trail.Count, trailLength)

    '    For i As Integer = 0 To count - 1

    '        ' Smooth exponential fade
    '        Dim t As Double = i / trailLength

    '        ' Capping alpha at 255 will make the trail more pronounced,
    '        ' Dim alpha As Integer = CInt(255 * t * t)
    '        ' but can be harsh. Capping at 32 gives a softer glow.
    '        Dim alpha As Integer = CInt(32 * t * t)


    '        If alpha > 255 Then alpha = 255

    '        trailBrushes(i).Color = Color.FromArgb(alpha, 0, 191, 255)

    '        Dim p As PointF = trail(i)
    '        Dim size As Integer = trailSizes(i)
    '        Dim offset As Single = trailOffsets(i)

    '        g.FillEllipse(trailBrushes(i),
    '                      p.X + offset,
    '                      p.Y + offset,
    '                      size,
    '                      size)
    '    Next

    'End Sub

    Private Sub DrawTrail(g As Graphics)

        Dim count As Integer = Math.Min(trail.Count, trailLength)

        For i As Integer = 0 To count - 1

            Dim p As PointF = trail(i)
            Dim size As Integer = trailSizes(i)
            Dim offset As Single = trailOffsets(i)

            g.FillEllipse(trailBrushes(i),
                      p.X + offset,
                      p.Y + offset,
                      size,
                      size)
        Next

    End Sub

    Private Sub DrawBall(g As Graphics)
        g.FillEllipse(ballBrush,
                      ballPos.X,
                      ballPos.Y,
                      ballDiameter,
                      ballDiameter)
    End Sub

    Private Sub DrawFPS(g As Graphics)
        UpdateFPS()
        g.DrawString($"FPS: {fps}", fpsFont, fpsBrush, 10, 10)
    End Sub

    Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
        ' Suppress background flicker
    End Sub

    ' -------------------------------
    '  FPS Counter
    ' -------------------------------
    Private Sub UpdateFPS()
        frameCount += 1

        If fpsTimer.ElapsedMilliseconds >= 1000 Then
            fps = frameCount
            frameCount = 0
            fpsTimer.Restart()
        End If
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)

        ' Ignore resize until resources are initialized
        If trailSizes Is Nothing OrElse trailOffsets Is Nothing Then
            Return
        End If

        ' Clamp ball inside new bounds
        If ballPos.X > ClientSize.Width - ballDiameter Then
            ballPos.X = ClientSize.Width - ballDiameter
        End If

        If ballPos.Y > ClientSize.Height - ballDiameter Then
            ballPos.Y = ClientSize.Height - ballDiameter
        End If

        ' Recompute offsets
        For i As Integer = 0 To trailLength - 1
            Dim size As Integer = trailSizes(i)
            trailOffsets(i) = CSng((ballDiameter - size) / 2)
        Next

        Invalidate()
    End Sub

    ' -------------------------------
    '  Cleanup
    ' -------------------------------
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing

        ballBrush?.Dispose()
        fpsBrush?.Dispose()
        fpsFont?.Dispose()
        physicsTimer?.Dispose()

        If trailBrushes IsNot Nothing Then
            For Each b In trailBrushes
                b?.Dispose()
            Next
        End If

        AudioPlayer.CloseAll()

    End Sub


    Private Sub CreateSoundFiles()

        Dim FilePath As String = Path.Combine(Application.StartupPath, "loop.mp3")

        CreateFileFromResource(FilePath, My.Resources.Resource1.BB_MegaLoop)

        FilePath = Path.Combine(Application.StartupPath, "bounce.mp3")

        CreateFileFromResource(FilePath, My.Resources.Resource1.Bounce)




    End Sub

    Private Sub CreateFileFromResource(filepath As String, resource As Byte())

        Try

            If Not IO.File.Exists(filepath) Then

                IO.File.WriteAllBytes(filepath, resource)

            End If

        Catch ex As Exception

            Debug.Print($"Error creating file: {ex.Message}")

        End Try

    End Sub


End Class
