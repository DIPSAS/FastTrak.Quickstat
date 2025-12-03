unit Emetra.VclForm.Period;

interface

uses
  Emetra.VclUtil.Style.Interfaces,
  Emetra.Classes.Transient,
  {General}
  Emetra.Settings.Interfaces,
  Emetra.Dictionary.Interfaces,
  {Standard}
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.StdCtrls, Vcl.Buttons, Vcl.WinXCalendars, Vcl.ExtCtrls, Vcl.Mask;

type
  TfrmPeriod = class( TForm, IGuiStyleObserver )
    btnOk: TBitBtn;
    btnCancel: TBitBtn;
    CalendarView1: TCalendarView;
    CalendarView2: TCalendarView;
    lblMainHeader: TLabel;
    lblBottomRightInfo: TLabel;
    lblSubheader: TLabel;
    lblZoomInfo: TLabel;
    panButtons: TPanel;
    panWhiteTop: TPanel;
  protected
    procedure UpdateStyle( Sender: IGuiStyle );
    procedure VerifyInput( Sender: TObject );
  public
    function TryGetPeriod( out APeriodStart, APeriodEnd: TDateTime ): boolean;
  end;

implementation

resourcestring
  rsValidInput =
  { } 'Angis som fra og med første dato (til venstre), '#10 +
  { } 'og til men ikke inkludert siste dato (til høyre).';
  rsInvalidInput =
  { } 'Siste dato må være etter første dato.'#10 +
  { } 'Merk at siste dato ikke er med i perioden.';


{$R *.dfm}
  { TfrmPeriod }

function TfrmPeriod.TryGetPeriod( out APeriodStart, APeriodEnd: TDateTime ): boolean;
begin
  CalendarView1.OnChange := VerifyInput;
  CalendarView2.OnChange := VerifyInput;
  Result := ( ShowModal = btnOk.ModalResult ) and ( CalendarView1.Date < CalendarView2.Date );
  APeriodStart := CalendarView1.Date;
  APeriodEnd := CalendarView2.Date;
end;

procedure TfrmPeriod.UpdateStyle( Sender: IGuiStyle );
begin
  Sender.StyleForm( Self );
  Sender.StyleTopPanel( panWhiteTop );
  Sender.StyleTopLabel( lblMainHeader );
  Sender.StyleButtonPanel( panButtons );
  Sender.StyleButton( btnOk );
  Sender.StyleButton( btnCancel );
  Sender.StyleInfoLabel( lblBottomRightInfo );
  Sender.StyleInfoLabel( lblSubheader );
  Sender.StyleInfoLabel( lblZoomInfo );
  lblSubheader.Font.Color := lblMainHeader.Font.Color;
  ClientWidth := CalendarView1.Width + CalendarView2.Width + CalendarView1.Left * 2 + CalendarView1.Margins.Right + CalendarView2.Margins.Left;
end;

procedure TfrmPeriod.VerifyInput( Sender: TObject );
begin
  btnOk.Enabled := ( CalendarView1.Date < CalendarView2.Date );
  if btnOk.Enabled then
    lblBottomRightInfo.Caption := rsValidInput
  else
    lblBottomRightInfo.Caption := rsInvalidInput;
end;

end.
