object frmPopulations: TfrmPopulations
  Left = 0
  Top = 0
  Width = 299
  Height = 547
  Color = clBtnFace
  ParentColor = False
  TabOrder = 0
  object panCheckBoxes: TPanel
    Left = 0
    Top = 48
    Width = 299
    Height = 28
    Align = alTop
    BevelEdges = [beBottom]
    BevelKind = bkTile
    BevelOuter = bvNone
    Caption = 'panCheckBoxes'
    ParentColor = True
    ShowCaption = False
    TabOrder = 0
    object cbShowCommon: TCheckBox
      AlignWithMargins = True
      Left = 3
      Top = 0
      Width = 272
      Height = 26
      Margins.Top = 0
      Margins.Bottom = 0
      Align = alLeft
      Caption = 'Vis de mest brukte'
      TabOrder = 0
    end
    object cbSimpleView: TCheckBox
      AlignWithMargins = True
      Left = 222
      Top = 0
      Width = 74
      Height = 26
      Margins.Top = 0
      Margins.Bottom = 0
      Align = alRight
      Alignment = taLeftJustify
      Caption = 'Forenklet'
      TabOrder = 1
    end
  end
  object panFilter: TPanel
    Left = 0
    Top = 0
    Width = 299
    Height = 48
    Align = alTop
    AutoSize = True
    BevelEdges = [beBottom]
    BevelKind = bkTile
    BevelOuter = bvNone
    Caption = 'panFilter'
    ParentColor = True
    ShowCaption = False
    TabOrder = 1
    object lblFilterHeader: TLabel
      AlignWithMargins = True
      Left = 3
      Top = 3
      Width = 80
      Height = 13
      Align = alTop
      Caption = 'Filter / s'#248'ketekst'
    end
    object edtPopFilter: TEdit
      AlignWithMargins = True
      Left = 3
      Top = 22
      Width = 293
      Height = 21
      Align = alTop
      TabOrder = 0
      TextHint = 'Skriv filter her'
    end
  end
  object splitMain: TRzSplitter
    Left = 0
    Top = 76
    Width = 299
    Height = 471
    FixedPane = fpLowerRight
    Orientation = orVertical
    ParentColor = True
    Position = 358
    Percent = 77
    HotSpotVisible = True
    HotSpotDirection = hsdMax
    SplitterWidth = 9
    Align = alClient
    TabOrder = 2
    BarSize = (
      0
      358
      299
      367)
    UpperLeftControls = (
      Bevel1)
    LowerRightControls = (
      memSourceCode)
    object Bevel1: TBevel
      Left = 0
      Top = 356
      Width = 299
      Height = 2
      Align = alBottom
      Shape = bsBottomLine
      ExplicitTop = 357
    end
    object memSourceCode: TMemo
      AlignWithMargins = True
      Left = 0
      Top = 1
      Width = 299
      Height = 103
      Margins.Left = 0
      Margins.Top = 1
      Margins.Right = 0
      Margins.Bottom = 0
      Align = alClient
      BevelEdges = [beTop]
      BevelKind = bkTile
      BorderStyle = bsNone
      ParentColor = True
      TabOrder = 0
    end
  end
end
