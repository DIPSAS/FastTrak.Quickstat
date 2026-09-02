{ ------------------------------------------------------------- }
{ Purpose : DIPS FastTrak UI }
{ By      : Bojan Nikolic }
{ For     : DIPS AS }
{ ------------------------------------------------------------- }
{ Copyright (C) DIPS AS 2020. All Rights Reserved. }
{ ------------------------------------------------------------- }
unit Emetra.Vcl.Intf;

interface

uses
  System.Classes, System.SysUtils,
  Vcl.Controls, Vcl.Graphics,
  {Winapi}
  Winapi.Windows,
  {Emetra.Vcl}
  Emetra.Vcl.Types;

type
  { IObjectConverter }

  IObjectConverter = interface
    ['{EF85FAF0-1FB3-4D5C-AE00-6C8170842EF2}']
    function ToObject: TObject;
  end;

  { IValueAccessors }

  IValueAccessors = interface
    ['{FB662DD3-D7F4-4009-B189-7E297519D524}']
    { Property Accessors }
    function GetAsBoolean: Boolean;
    function GetAsDate: TDate;
    function GetAsDateTime: TDateTime;
    function GetAsFloat: Double;
    function GetAsInteger: Integer;
    function GetAsString: string;
    procedure SetAsBoolean( const Value: Boolean );
    procedure SetAsDate( const Value: TDate );
    procedure SetAsDateTime( const Value: TDateTime );
    procedure SetAsFloat( const Value: Double );
    procedure SetAsInteger( const Value: Integer );
    procedure SetAsString( const Value: string );
    { Properties }
    property AsBoolean: Boolean read GetAsBoolean write SetAsBoolean;
    property AsDate: TDate read GetAsDate write SetAsDate;
    property AsDateTime: TDateTime read GetAsDateTime write SetAsDateTime;
    property AsFloat: Double read GetAsFloat write SetAsFloat;
    property AsInteger: Integer read GetAsInteger write SetAsInteger;
    property AsString: string read GetAsString write SetAsString;
  end;

  { ICheckPopupOwner }

  ICheckPopupOwner = interface
    ['{7ECA25DE-78FF-4B7D-8629-69D452968597}']
    procedure CheckBoxClick( const Index: Integer );
  end;

  { IWinControl }

  IWinControl = interface
    ['{BE7FAE6C-F3E4-499C-9305-FA6349DF2BD9}']
    function Perform( Msg: Cardinal; WParam: WParam; LParam: LParam ): LRESULT;
  end;

  { IMouseDispatch }

  IMouseDispatch = interface
    ['{163A2B28-1AC4-446C-9EE0-2670E7DEBDE0}']
    procedure DispatchMouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer; Parent: TWinControl );
  end;

  { IInplaceEdit }

  IInplaceEdit = interface
    ['{A8A2B25D-8F7F-4F73-BE06-2783DDE00652}']
    function CanDispatchKey( const Key: Word ): Boolean;
    function Focused: Boolean;
    procedure DoExit;
    function GetColor: TColor;
    function GetHandle: HWND;
    function GetIsInteracting: Boolean;
    function GetShowing: Boolean;
    function GetText: TCaption;
    function GetBoundsRect: TRect;
    procedure Assign( Source: TPersistent );
    procedure Invalidate;
    function Perform( Msg: Cardinal; WParam: WParam; LParam: LParam ): LRESULT;
    procedure SetBounds( ALeft, ATop, AWidth, AHeight: Integer );
    procedure SetColor( const Value: TColor );
    procedure SetInplaceEdit( const Value: Boolean );
    procedure SetParent( AParent: TWinControl );
    procedure SetFocus;
    procedure SetFont( Value: TFont );
    procedure SetOnChange( const Value: TNotifyEvent );
    procedure SetText( const Value: TCaption );
    property Color: TColor read GetColor write SetColor;
    property Font: TFont write SetFont;
    property Handle: HWND read GetHandle;
    property InplaceEdit: Boolean write SetInplaceEdit;
    property IsInteracting: Boolean read GetIsInteracting;
    property OnChange: TNotifyEvent write SetOnChange;
    property Parent: TWinControl write SetParent;
    property Showing: Boolean read GetShowing;
    property Text: TCaption read GetText write SetText;
  end;

  { IHeightChange }

  IHeightChange = interface
    ['{FA7F03BF-43F5-42C0-B9BD-B43A65AE2691}']
    { Property Accessors }
    procedure SetOnHeightChange( const Value: TNotifyEvent );
    { Properties }
    property OnHeightChange: TNotifyEvent write SetOnHeightChange;
  end;

  { IEditButton }

  IEditButton = interface
    ['{57DD4C89-D01E-45FD-A9DB-EAD364587D52}']
    { Property Accessors }
    function GetAlign: TAlign;
    function GetAutoUp: boolean;
    function GetBoundsRect: TRect;
    function GetCaption: string;
    function GetDown: boolean;
    function GetEdit: IInplaceEdit;
    function GetHandle: HWND;
    function GetHover: boolean;
    function GetLeft: Integer;
    function GetOnButtonClick: TNotifyEvent;
    function GetOnDown: TNotifyEvent;
    function GetOnUp: TNotifyEvent;
    function GetParent: TWinControl;
    function GetVisible: boolean;
    function GetWidth: Integer;
    procedure SetAlign( const Value: TAlign );
    procedure SetAutoUp( const Value: boolean );
    procedure SetCaption( const Value: string );
    procedure SetDown( const Value: boolean );
    procedure SetEdit( const Value: IInplaceEdit );
    procedure SetHover( const Value: boolean );
    procedure SetLeft( const Value: Integer );
    procedure SetOnButtonClick( const Value: TNotifyEvent );
    procedure SetOnDown( const Value: TNotifyEvent );
    procedure SetOnUp( const Value: TNotifyEvent );
    procedure SetParent( AParent: TWinControl );
    procedure SetVisible( const Value: boolean );
    procedure SetWidth( const Value: Integer );
    { Methods }
    procedure Invalidate;
    function ParentToClient(const Point: TPoint; AParent: TWinControl = nil): TPoint;
    procedure Refresh;
    { Properties }
    property Align: TAlign read GetAlign write SetAlign;
    property AutoUp: boolean read GetAutoUp write SetAutoUp;
    property BoundsRect: TRect read GetBoundsRect;
    property Caption: string read GetCaption write SetCaption;
    property Down: boolean read GetDown write SetDown;
    property Edit: IInplaceEdit read GetEdit write SetEdit;
    property Handle: HWND read GetHandle;
    property Hover: boolean read GetHover write SetHover;
    property Left: Integer read GetLeft write SetLeft;
    property Parent: TWinControl read GetParent write SetParent;
    property Visible: boolean read GetVisible write SetVisible;
    property Width: Integer read GetWidth write SetWidth;
    { Events }
    property OnButtonClick: TNotifyEvent read GetOnButtonClick write SetOnButtonClick;
    property OnDown: TNotifyEvent read GetOnDown write SetOnDown;
    property OnUp: TNotifyEvent read GetOnUp write SetOnUp;
  end;

  { ISelectNotify }

  ISelectNotify = interface
    ['{78BB90A1-FC5E-4ED1-B8CE-16042E49B337}']
    { Property Accessors }
    function GetOnSelect: TNotifyEvent;
    procedure SetOnSelect( const Value: TNotifyEvent );
    { Properties }
    property OnSelect: TNotifyEvent read GetOnSelect write SetOnSelect;
  end;

  { IDropDown }

  IDropDown = interface
    ['{5840F233-921B-4DDA-9EEB-830D910459E3}']
    { Property Accessors }
    function GetAutoClose: Boolean;
    function GetDroppedDown: Boolean;
    procedure SetDroppedDown( const Value: Boolean );
    { Methods }
    procedure DoCloseUp;
    procedure DoSelect;
    { Properties }
    property AutoClose: Boolean read GetAutoClose;
    property DroppedDown: Boolean read GetDroppedDown write SetDroppedDown;
  end;

  { IDropDownNotify }

  IDropDownNotify = interface
    ['{4BE4B134-C812-46EC-966F-B69A440C9C0A}']
    { Property Accessors }
    procedure SetOnDropDown( const Value: TNotifyEvent );
    { Properties }
    property OnDropDown: TNotifyEvent write SetOnDropDown;
  end;

  { IStyledDropDown }

  IStyledDropDown = interface
    ['{E5421D49-ADE6-482B-81A0-F739EDDD3FE2}']
    procedure SetStyle( const Value: TNxDropDownStyle );
  end;

  { IStringsControl }

  IStringsControl = interface
    ['{FBB198D8-A097-4AD5-9BDB-CA725C77226B}']
    function GetAutoComplete: Boolean;
    function GetDropDownCount: Integer;
    function GetItemHeight: Integer;
    function GetItemIndex: Integer;
    function GetItems: TStrings;
    function GetItemsAlignment: TAlignment;
    function GetOldItemIndex: Integer;
    procedure SetAutoComplete( const Value: Boolean );
    procedure SetDropDownCount( const Value: Integer );
    procedure SetItemHeight( const Value: Integer );
    procedure SetItemIndex( const Value: Integer );
    procedure SetItems( const Value: TStrings );
    procedure SetItemsAlignment( const Value: TAlignment );
    { Methods }
    function MeasureItemHeight( const Index: Integer; const AWidth: Integer ): Integer;
    function PaintCell( const Index: Integer; CellRect: TRect; State: TOwnerDrawState ): Boolean;
    procedure TryItemIndex( const Index: Integer; Select: Boolean );
    { Properties }
    property AutoComplete: Boolean read GetAutoComplete write SetAutoComplete;
    property DropDownCount: Integer read GetDropDownCount write SetDropDownCount;
    property ItemHeight: Integer read GetItemHeight write SetItemHeight;
    property ItemIndex: Integer read GetItemIndex write SetItemIndex;
    property Items: TStrings read GetItems write SetItems;
    property ItemsAlignment: TAlignment read GetItemsAlignment write SetItemsAlignment;
    property OldItemIndex: Integer read GetOldItemIndex;
  end;

  { IView }

  INxView = interface( IObjectConverter )
    ['{D6A8F78E-BBF5-4D8D-A58C-84B6C705F666}']
    { Property Getters }
    function GetCanvas: TCanvas;
    function GetClientRect: TRect;
    { Property Setters }
    procedure SetCanvas( const Value: TCanvas );
    procedure SetClientRect( const Value: TRect );
    { Methods }
    procedure Paint;
    { Properties }
    property Canvas: TCanvas read GetCanvas write SetCanvas;
    property ClientRect: TRect read GetClientRect write SetClientRect;
  end;

  { IViewOwner }

  IViewOwner = interface
    ['{1FBB192B-8F43-468D-B949-4AD97FB2BD4C}']
    function GetCanvas: TCanvas;
    function GetHandle: THandle;
    { Methods }
    procedure InvalidateRect( const Source: TRect );
    { Properties }
    property Canvas: TCanvas read GetCanvas;
    property Handle: THandle read GetHandle;
  end;

  { IObjectView }

  IObjectView = interface( INxView )
    ['{A9D2227E-1140-4F32-8783-F5A333A094BD}']
    { Property Accessors }
    function GetAsString: WideString;
    function GetControl: TWinControl;
    function GetFont: TFont;
    procedure SetAsString( const Value: WideString );
    procedure SetControl( const Value: TWinControl );
    procedure SetFont( const Value: TFont );
    { Methods }
    procedure Assign( Source: TObject );
    function CanDispatchKey( var Key: Word ): Boolean;
    procedure KeyDown( var Key: Word; Shift: TShiftState );
    procedure KeyUp; overload;
    procedure KeyUp( var Key: Word; Shift: TShiftState ); overload;
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
    procedure MouseLeave;
    procedure MouseMove( Shift: TShiftState; X, Y: Integer );
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); overload;
    procedure MouseUp; overload;
    { Properties }
    property AsString: WideString read GetAsString write SetAsString;
    property Control: TWinControl read GetControl write SetControl;
    property Font: TFont read GetFont write SetFont;
  end;

  { IValidatable }

  IValidatable = interface
    ['{9664A6C9-5351-4F01-9B33-F5FFC2F2F506}']
    { Property Accessors }
    procedure SetValidationResult( const Value: TValidationResult );
    { Methods }
    function Validate( const Func: TFunc<Boolean> ): TValidationResult;
    { Properties }
    property ValidationResult: TValidationResult write SetValidationResult;
  end;

  { IValidator }

  IValidator = interface
    ['{E8280A04-1A31-4721-83E9-0502DAFB3FB5}']
    function Validate( const Validatable: IValidatable ): TValidationResult;
    function GetErrorCount: Integer;
    property ErrorCount: Integer read GetErrorCount;
  end;

  { INxPreviewDrawable }

  TPreviewDrawEvent = procedure( Sender: TObject; PreviewRect: TRect ) of object;

  IPreviewDrawable = interface
    ['{7F413D49-FD91-4423-A698-9814C1E78C94}']
    { Property Accessors }
    function GetOnPreviewDraw: TPreviewDrawEvent;
    procedure SetOnPreviewDraw( const Value: TPreviewDrawEvent );
    { Properties }
    property OnPreviewDraw: TPreviewDrawEvent read GetOnPreviewDraw write SetOnPreviewDraw;
  end;

  { IPopupControl }

  IPopupControl = interface
    ['{B7D52F81-F7E6-47C5-8D25-4FB0746B6A50}']
    function GetCanvas: TCanvas;
    function GetFont: TFont;
    procedure KeyDown( var Key: Word; Shift: TShiftState );
    procedure Popup( const X, Y: Integer; Anchor: TPopupAnchor = paLeft ); overload;
    procedure Popup( const Location: TPoint; Anchor: TPopupAnchor = paLeft ); overload;
    procedure SetDropDown( const Value: IDropDown );
    procedure UpdatePopupBounds;
    procedure UpdatePopup;
    { Properties }
    property Canvas: TCanvas read GetCanvas;
    property Font: TFont read GetFont;
  end;

  { IListPopupControl }

  IListPopupControl = interface
    ['{7CE7DB79-761B-450D-9A21-BB3BB0BBCFA2}']
    procedure ScrollDown;
    procedure ScrollUp;
  end;

  { IListPopup }

  IListPopup = interface
    ['{36DF43D6-E0C5-4929-8A61-D505C8614E44}']
    { Property Accessors }
    function Get_ListWidth: Integer;
    { Properties }
    property ListWidth: Integer read Get_ListWidth;
  end;

  { IOwnerDrawn }

  IOwnerDrawn = interface
    ['{3C7387EA-640C-4DF6-8F9F-1D3EACF033FA}']
    function GetDrawingOptions: TNxDrawingOptions;
    property DrawingOptions: TNxDrawingOptions read GetDrawingOptions;
  end;

  { ITextControl }

  ITextControl = interface
    ['{EC1B20D0-D182-43A8-BEBA-22338F85E90A}']
    function GetText: TCaption;
    property Text: TCaption read GetText;
  end;

  { IButtonEdit }

  IButtonEdit = interface
    ['{72798DF5-D921-41E0-856D-AB9FF32BC33B}']
    procedure SetOnButtonClick( const Value: TNotifyEvent );
  end;

implementation

end.
