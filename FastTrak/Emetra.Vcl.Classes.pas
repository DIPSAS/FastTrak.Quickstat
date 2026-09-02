unit Emetra.Vcl.Classes;

interface

uses
  System.Classes, System.Types,
  Vcl.Graphics, Vcl.Controls,
  Winapi.Windows,
  {Emetra.Vcl}
  Emetra.Vcl.Intf;

type
  TNxObjectView = class( TInterfacedObject, IObjectView )
  private
    { Fields }
    FCanvas: TCanvas;
    FClientRect: TRect;
    FControl: TWinControl;
    FFont: TFont;
  protected
    { Property Getters }
    function GetCanvas: TCanvas;
    function GetClientRect: TRect;
    function GetControl: TWinControl;
    function GetFont: TFont;
    { Property Setters }
    procedure SetCanvas( const Value: TCanvas );
    procedure SetClientRect( const Value: TRect );
    procedure SetControl( const Value: TWinControl );
    procedure SetFont( const Value: TFont );
  protected
    function ToObject: TObject;
    { Virtual Methods }
    function GetAsString: WideString; virtual;
    procedure InvalidateRect( InvalidationRect: TRect );
    procedure SetAsString( const Value: WideString ); virtual;
  public
    procedure Assign( Source: TObject ); virtual;
    function CanDispatchKey( var Key: Word ): Boolean; virtual;
    destructor Destroy; override;
    { Keyboard Methods }
    procedure KeyDown( var Key: Word; Shift: TShiftState ); virtual;
    procedure KeyUp; overload; virtual;
    procedure KeyUp( var Key: Word; Shift: TShiftState ); overload; virtual;
    { Mouse Methods }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); virtual;
    procedure MouseLeave; virtual;
    procedure MouseMove( Shift: TShiftState; X, Y: Integer ); virtual;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); overload; virtual;
    procedure MouseUp; overload; virtual;
    { Painting Methods }
    procedure Paint; virtual;
    { Properties }
    property Canvas: TCanvas read GetCanvas write SetCanvas;
    property ClientRect: TRect read GetClientRect write SetClientRect;
    property Control: TWinControl read GetControl write SetControl;
    property Font: TFont read GetFont write SetFont;
  end;

implementation

uses
  {Standard}
  SysUtils;

{ TNxObjectView }

procedure TNxObjectView.Assign( Source: TObject );
begin
  inherited;
  if Source is TNxObjectView then
  begin
    Canvas := TNxObjectView( Source ).Canvas;
    ClientRect := TNxObjectView( Source ).ClientRect;
    Control := TNxObjectView( Source ).Control;
  end;
end;

function TNxObjectView.CanDispatchKey( var Key: Word ): Boolean;
begin
  Result := True;
end;

destructor TNxObjectView.Destroy;
begin
  FreeAndNil( FFont );
  inherited;
end;

function TNxObjectView.GetAsString: WideString;
begin

end;

function TNxObjectView.GetCanvas: TCanvas;
begin
  Result := FCanvas;
end;

function TNxObjectView.GetClientRect: TRect;
begin
  Result := FClientRect;
end;

function TNxObjectView.GetControl: TWinControl;
begin
  Result := FControl;
end;

function TNxObjectView.GetFont: TFont;
begin
  Result := FFont;
end;

procedure TNxObjectView.InvalidateRect( InvalidationRect: TRect );
begin
  Winapi.Windows.InvalidateRect( Control.Handle, InvalidationRect, False );
end;

procedure TNxObjectView.KeyDown( var Key: Word; Shift: TShiftState );
begin

end;

procedure TNxObjectView.KeyUp;
begin

end;

procedure TNxObjectView.KeyUp( var Key: Word; Shift: TShiftState );
begin

end;

procedure TNxObjectView.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin

end;

procedure TNxObjectView.MouseLeave;
begin

end;

procedure TNxObjectView.MouseMove( Shift: TShiftState; X, Y: Integer );
begin

end;

procedure TNxObjectView.MouseUp;
begin

end;

procedure TNxObjectView.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin

end;

procedure TNxObjectView.Paint;
begin

end;

procedure TNxObjectView.SetAsString( const Value: WideString );
begin

end;

procedure TNxObjectView.SetCanvas( const Value: TCanvas );
begin
  FCanvas := Value;
end;

procedure TNxObjectView.SetClientRect( const Value: TRect );
begin
  FClientRect := Value;
end;

procedure TNxObjectView.SetControl( const Value: TWinControl );
begin
  FControl := Value;
end;

procedure TNxObjectView.SetFont( const Value: TFont );
begin
  if FFont = nil then
    FFont := TFont.Create;
  FFont.Assign( Value );
end;

function TNxObjectView.ToObject: TObject;
begin
  Result := Self;
end;

end.
