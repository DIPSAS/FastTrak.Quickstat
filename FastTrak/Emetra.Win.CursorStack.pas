unit Emetra.Win.CursorStack;

interface

uses
  Vcl.Controls, Vcl.Forms,
  Generics.Collections;

type
  TCursorStack = class( TObject )
  strict private
    fStack: TStack<TCursor>;
  public
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure Push( const ACursor: TCursor );
    procedure PushWait;
    procedure Pop;
  end;

var
  CursorStack: TCursorStack;

implementation

{ TCursorStack }

procedure TCursorStack.Push( const ACursor: TCursor );
begin
  fStack.Push( Screen.Cursor );
  Screen.Cursor := ACursor;
end;

procedure TCursorStack.PushWait;
begin
  Push( crHourGlass );
end;

procedure TCursorStack.AfterConstruction;
begin
  inherited;
  fStack := TStack<TCursor>.Create;
end;

procedure TCursorStack.BeforeDestruction;
begin
  fStack.Free;
  inherited;
end;

procedure TCursorStack.Pop;
begin
  Screen.Cursor := fStack.Pop;
end;

initialization

CursorStack := TCursorStack.Create;

finalization

CursorStack.Free;

end.
