unit Emetra.VclForm.EditAndMemo;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes, Vcl.Graphics,
  Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.StdCtrls, Vcl.Buttons, Vcl.ExtCtrls;

type
  TfrmSaveSpec = class(TForm)
    Panel1: TPanel;
    Panel2: TPanel;
    Image1: TImage;
    hdrSaveSpec: TLabel;
    Label1: TLabel;
    memComment: TMemo;
    panButtons: TPanel;
    Label2: TLabel;
    btnSave: TBitBtn;
    btnClose: TBitBtn;
    edtTitle: TEdit;
  private
    function Get_Comment: string;
    function Get_Title: string;
    { Private declarations }
  public
    { Public declarations }
    procedure Clear;
    procedure SetHeader( const AHeader: string );
    { Properties }
    property Comment: string read Get_Comment;
    property Title: string read Get_Title;
  end;

var
  frmSaveSpec: TfrmSaveSpec;

implementation

{$R *.dfm}

{ TForm1 }

procedure TfrmSaveSpec.Clear;
begin
  memComment.Clear;
  edtTitle.Clear;
end;

function TfrmSaveSpec.Get_Comment: string;
begin
  Result := memComment.Text;
end;

function TfrmSaveSpec.Get_Title: string;
begin
  Result := edtTitle.Text;
end;

procedure TfrmSaveSpec.SetHeader(const AHeader: string);
begin
  hdrSaveSpec.Caption := AHEader;
  Self.Caption := AHeader;
end;

end.
