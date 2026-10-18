$ErrorActionPreference = 'Stop'

$deckPath = Join-Path $PSScriptRoot 'Chapter 5-9 Unity Project Labs.pptx'

function Get-Rgb([string]$Hex) {
    $value = $Hex.TrimStart('#')
    $red = [Convert]::ToInt32($value.Substring(0, 2), 16)
    $green = [Convert]::ToInt32($value.Substring(2, 2), 16)
    $blue = [Convert]::ToInt32($value.Substring(4, 2), 16)
    return ($red + ($green * 256) + ($blue * 65536))
}

function Add-Text($Slide, [string]$Text, [double]$X, [double]$Y, [double]$Width, [double]$Height, [double]$Size, [string]$Color, [bool]$Bold = $false, [string]$Font = 'Aptos') {
    $shape = $Slide.Shapes.AddTextbox(1, $X, $Y, $Width, $Height)
    $shape.Line.Visible = 0
    $shape.Fill.Visible = 0
    $shape.TextFrame.MarginLeft = 0
    $shape.TextFrame.MarginRight = 0
    $shape.TextFrame.MarginTop = 0
    $shape.TextFrame.MarginBottom = 0
    $shape.TextFrame.WordWrap = -1
    $shape.TextFrame.TextRange.Text = $Text
    $shape.TextFrame.TextRange.Font.Name = $Font
    $shape.TextFrame.TextRange.Font.Size = $Size
    $shape.TextFrame.TextRange.Font.Bold = $Bold
    $shape.TextFrame.TextRange.Font.Color.RGB = Get-Rgb $Color
    return $shape
}

function Add-Rect($Slide, [double]$X, [double]$Y, [double]$Width, [double]$Height, [string]$Color, [int]$ShapeType = 1) {
    $shape = $Slide.Shapes.AddShape($ShapeType, $X, $Y, $Width, $Height)
    $shape.Fill.ForeColor.RGB = Get-Rgb $Color
    $shape.Line.Visible = 0
    return $shape
}

function New-Slide([string]$Background) {
    $slide = $presentation.Slides.Add($presentation.Slides.Count + 1, 12)
    Add-Rect $slide 0 0 960 540 $Background | Out-Null
    return $slide
}

function Add-Footer($Slide, [int]$Number) {
    Add-Rect $Slide 48 500 864 1 '#D5DCD6' | Out-Null
    Add-Text $Slide 'CIST 1423  /  UNITY XR LABS' 48 511 700 16 9 '#55645D' $true | Out-Null
    Add-Text $Slide ('0' + $Number) 865 508 45 20 11 '#55645D' $true 'Aptos Display' | Out-Null
}

function Add-LabSlide([int]$Number, [string]$Week, [string]$Chapter, [string]$Title, [string]$Subtitle, [string]$Accent, [string[]]$Build, [string]$TestTitle, [string[]]$Tests, [string]$Deliverable) {
    $slide = New-Slide '#F3F1E9'
    Add-Rect $slide 0 0 14 540 $Accent | Out-Null
    Add-Text $slide ($Week + '  /  ' + $Chapter) 56 35 700 20 11 $Accent $true | Out-Null
    Add-Text $slide $Title 56 67 840 42 29 '#1A302A' $true 'Aptos Display' | Out-Null
    Add-Text $slide $Subtitle 58 119 820 32 14 '#55645D' | Out-Null
    Add-Rect $slide 56 174 526 270 '#FFFEFA' | Out-Null
    Add-Text $slide 'STUDENTS BUILD' 78 194 450 18 10 $Accent $true | Out-Null
    Add-Text $slide ($Build -join "`n") 78 225 475 188 16 '#263A33' | Out-Null
    Add-Rect $slide 608 174 296 282 '#1D3D35' | Out-Null
    Add-Text $slide $TestTitle 630 195 246 22 12 '#F2C16D' $true | Out-Null
    Add-Text $slide ($Tests -join "`n") 630 231 248 158 14 '#F7F4EC' | Out-Null
    Add-Text $slide 'TURN IN' 630 396 100 16 9 '#AFC7BD' $true | Out-Null
    Add-Text $slide $Deliverable 630 415 248 38 11 '#FFFFFF' $true | Out-Null
    Add-Footer $slide $Number
}

$powerPoint = $null
$presentation = $null
try {
    $powerPoint = New-Object -ComObject PowerPoint.Application
    $powerPoint.Visible = -1
    $presentation = $powerPoint.Presentations.Add()
    $presentation.PageSetup.SlideWidth = 960
    $presentation.PageSetup.SlideHeight = 540

    $slide = New-Slide '#173C35'
    Add-Rect $slide 692 0 268 540 '#E5A64B' | Out-Null
    Add-Rect $slide 724 60 182 420 '#F5F1E8' | Out-Null
    Add-Text $slide 'CIST 1423  /  FALL 2026' 64 58 500 22 12 '#F2C16D' $true | Out-Null
    Add-Text $slide "XR PROJECT`nLABS" 64 112 590 150 44 '#FFFDF7' $true 'Aptos Display' | Out-Null
    Add-Text $slide 'Chapters 5-9 in Unity' 66 286 540 40 25 '#F7F4EC' $false 'Aptos Display' | Out-Null
    Add-Text $slide 'Five buildable labs. One path from interaction to evidence.' 66 342 535 56 17 '#C8D8D0' | Out-Null
    $weekY = @(86, 163, 240, 317, 394)
    $weekName = @('WEEK 09  /  INTERACTIVE VR', 'WEEK 10  /  INTERACTIVE AR', 'WEEK 11  /  SOUND + VFX', 'WEEK 12  /  ADVANCED XR', 'WEEK 13  /  BEST PRACTICES')
    $weekColor = @('#E46F4F', '#4C82A5', '#D49B2E', '#348775', '#745C3B')
    for ($index = 0; $index -lt 5; $index++) {
        Add-Rect $slide 744 $weekY[$index] 140 52 $weekColor[$index] 5 | Out-Null
        Add-Text $slide $weekName[$index] 754 ($weekY[$index] + 16) 122 28 10 '#FFFFFF' $true | Out-Null
    }
    Add-Text $slide 'Unity 2022.3.62f3  |  Source-first examples' 66 459 570 22 12 '#AFC7BD' | Out-Null

    $slide = New-Slide '#F3F1E9'
    Add-Text $slide 'THE LEARNING ARC' 55 38 400 18 11 '#317263' $true | Out-Null
    Add-Text $slide 'Build, test, then make the evidence visible' 55 70 835 50 30 '#1A302A' $true 'Aptos Display' | Out-Null
    Add-Text $slide 'Each lab adds one XR capability, then asks students to verify it in a fallback or target-device workflow.' 55 126 830 35 15 '#55645D' | Out-Null
    $arcX = @(58, 240, 422, 604, 786)
    $arcColor = @('#E46F4F', '#4C82A5', '#D49B2E', '#348775', '#745C3B')
    $arcTitle = @('INTERACT', 'PLACE', 'FEEL', 'EXTEND', 'REVIEW')
    $arcWeek = @('09  /  VR', '10  /  AR', '11  /  AUDIO', '12  /  GAZE', '13  /  UX')
    for ($index = 0; $index -lt 5; $index++) {
        Add-Rect $slide $arcX[$index] 222 146 142 '#FFFEFA' | Out-Null
        Add-Rect $slide $arcX[$index] 222 146 8 $arcColor[$index] | Out-Null
        Add-Text $slide $arcWeek[$index] ($arcX[$index] + 14) 247 120 18 10 '#66746E' $true | Out-Null
        Add-Text $slide $arcTitle[$index] ($arcX[$index] + 14) 280 130 26 17 '#1A302A' $true 'Aptos Display' | Out-Null
    }
    Add-Text $slide 'ASSESSMENT THREAD' 58 408 220 18 10 '#317263' $true | Out-Null
    Add-Text $slide 'Interaction diagram  ->  device/editor test  ->  observed result  ->  revision + peer review' 58 434 820 30 16 '#1A302A' $true | Out-Null
    Add-Footer $slide 2

    Add-LabSlide 3 'WEEK 09' 'CHAPTER 5' 'Interactive VR' 'Animate a prop with an Inspector event; reach another target with controller-ray C#.' '#E46F4F' @('- UI Button -> Animator trigger -> one-shot pulse', '- XR controller ray toggles a target color', '- Mouse + Space provide desktop fallbacks', '- No opaque assets or custom button script') 'VERIFY BOTH PATHS' @('1  Click the button; confirm the prop returns to Idle.', '2  Aim and trigger; confirm target changes.', '3  Repeat in mouse and keyboard fallback.') 'Two interactions + event-path diagram'
    Add-LabSlide 4 'WEEK 10' 'CHAPTER 6' 'Interactive AR' 'Build a tabletop placement flow with touch, C# selection, and a compact control UI.' '#4C82A5' @('- Detect a plane and place a primitive prefab', '- Tap to select; swap, delete, reset, toggle planes', '- AR Foundation 5.1.5 with ARCore / ARKit providers', '- Editor mouse + ground-plane fallback') 'VERIFY THE LOOP' @('1  Scan until a plane is tracked; place a prefab.', '2  Select it and swap variants in place.', '3  Check delete, reset, and plane visibility.') 'Device/Editor evidence + reflection'
    Add-LabSlide 5 'WEEK 11' 'CHAPTER 7' 'Sound and VFX' 'Resonance Garden turns distance and contact into spatial sound and particle feedback.' '#D49B2E' @('- Three procedural tones at distinct pitches', '- AudioSources spatialized and routed to a mixer group', '- Slider controls exposed feedback level', '- Trigger entry plays a particle burst once') 'LISTEN + OBSERVE' @('1  Enter each beacon; compare pitch and burst.', '2  Move away; listen for distance attenuation.', '3  Lower mixer to -30 dB; compare level.') 'Evidence of pitch, distance, and mix tests'
    Add-LabSlide 6 'WEEK 12' 'CHAPTER 8' 'Advanced XR: head-gaze dwell' 'A camera ray and dwell timer create a hardware-neutral gaze interaction.' '#348775' @('- Look at a collider target for 1.2 seconds', '- Fill indicator tracks dwell progress', '- Completion event drives selection feedback', '- Mouse-position fallback works without headset') 'STATE THE LIMIT' @('1  Leave early; progress resets without selection.', '2  Hold gaze; event fires once at completion.', '3  Report device and package versions tested.') 'Test checklist + limitation note (not multiplayer)'
    Add-LabSlide 7 'WEEK 13' 'CHAPTER 9' 'Comfort, validation, and review' 'Apply lifecycle thinking to a prior project; the helper records observations, not comfort certification.' '#745C3B' @('- Review locomotion, reach, UI, access, recovery, performance', '- Write observation -> impact -> change -> retest', '- Compare toolkits using dated primary sources', '- Make one focused change and review a peer PR') 'EVIDENCE OVER CLAIMS' @('1  Test common tasks on intended setup.', '2  Record before/after result and limitations.', '3  Reviewer checks evidence and regression risk.') 'Revised scene + report + toolkit note + peer review'

    $slide = New-Slide '#173C35'
    Add-Text $slide 'THE FIVE PROJECTS, ONE PRACTICE' 58 43 820 18 11 '#F2C16D' $true | Out-Null
    Add-Text $slide 'Make every claim testable.' 58 80 820 52 34 '#FFFDF7' $true 'Aptos Display' | Out-Null
    Add-Text $slide 'The sequence moves from authored interaction to device-aware validation and collaborative review.' 58 142 820 38 16 '#C8D8D0' | Out-Null
    $row = @(@('09', 'Input and event paths'), @('10', 'Placement and selection'), @('11', 'Spatial feedback'), @('12', 'Gaze and dwell'), @('13', 'Comfort and iteration'))
    $rowColor = @('#E46F4F', '#4C82A5', '#D49B2E', '#348775', '#B28B58')
    $y = 214
    for ($index = 0; $index -lt 5; $index++) {
        Add-Rect $slide 62 $y 832 44 '#224B42' | Out-Null
        Add-Rect $slide 62 $y 54 44 $rowColor[$index] | Out-Null
        Add-Text $slide $row[$index][0] 76 ($y + 13) 36 20 12 '#FFFFFF' $true | Out-Null
        Add-Text $slide $row[$index][1] 136 ($y + 11) 600 24 16 '#FFFFFF' $true | Out-Null
        $y += 52
    }
    Add-Text $slide 'START HERE' 62 486 96 18 10 '#F2C16D' $true | Out-Null
    Add-Text $slide 'Open Week-09 through Week-13 folders; each README includes setup, tests, and submission evidence.' 164 482 730 28 13 '#F7F4EC' | Out-Null

    $presentation.SaveAs($deckPath, 24)
    Write-Output "Created $deckPath with $($presentation.Slides.Count) slides."
    $presentation.Close()
} finally {
    if ($presentation -ne $null) { [void][Runtime.InteropServices.Marshal]::ReleaseComObject($presentation) }
    if ($powerPoint -ne $null) { $powerPoint.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($powerPoint) }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}