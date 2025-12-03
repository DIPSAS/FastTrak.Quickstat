unit Emetra.VclUtil.Spotlight;

interface

uses
  Emetra.Classes.Subject,
  Emetra.Logging.Interfaces,
  { Standard }
  Classes, Contnrs, StdCtrls, ComCtrls, ExtCtrls;

type
  TSpotLightContext = class(TExposedLogged)
  private
    FListBox: TCustomListBox;
    FSimpleView: TCheckBox;
    FList: TList;
    FCurrSelection: TObjectList;
    FEditFilter: TEdit;
    FOldFilter: string;
    FShowAll: TCheckBox;
    FSmallLabel: TLabel;
    FHeaderPanel: TPanel;
    FHeaderLabel: TLabel;
    FProgressBar: TProgressBar;
    procedure RefreshFilter;
    function VerifyFilter: boolean;
    procedure FilterChanged( Sender: TObject );
    procedure InternalRefreshList(Sender: TObject; AList: TList );
  public
    constructor Create(AListBox: TCustomListBox; AList: TList; AFilter: TEdit; ASimpleView, AShowAll: TCheckBox; ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure SetHeader( AHeaderPanel: TPanel; AHeaderLabel, ASmallLabel: TLabel );
    procedure RefreshList(Sender: TObject );
    property ProgressBar: TProgressBar read FProgressBar write FProgressBar;
  end;

implementation

uses
  Emetra.VclUtil.Style,
  Emetra.VclUtil.Style.Interfaces,
  Emetra.Interfaces.ListBox,
  Graphics, Forms, SysUtils;

constructor TSpotLightContext.Create(AListBox: TCustomListBox; AList: TList; AFilter: TEdit;
  ASimpleView, AShowAll: TCheckBox; ALog: ILog );
begin
  inherited Create( ALog );
  FListBox := AListBox;
  FList := AList;
  FShowAll := AShowAll;
  FEditFilter := AFilter;
  FSimpleView := ASimpleView;
end;

procedure TSpotLightContext.AfterConstruction;
begin
  inherited;
  FCurrSelection := TObjectList.Create( false );
  if Assigned( FEditFilter ) then
    FEditFilter.OnChange := Self.FilterChanged;
  if Assigned( FSimpleView ) then
    FSimpleView.OnClick := Self.RefreshList;
  if Assigned( FShowAll ) then
    FShowAll.OnClick := Self.RefreshList;
  FHeaderPanel := nil;
  FHeaderLabel := nil;
  FSmallLabel := nil;
  FProgressBar := nil;
end;

procedure TSpotLightContext.BeforeDestruction;
begin
  FreeAndNil( FCurrSelection );
  inherited;
end;

function TSpotLightContext.VerifyFilter: boolean;
begin
  Result := Assigned(FList) and Assigned(FListBox) and Assigned(FHeaderPanel) and Assigned(FSmallLabel) and
    Assigned(FHeaderLabel);
end;

procedure TSpotLightContext.FilterChanged(Sender: TObject);
var
  newFilter: string;
begin
  newFilter := Trim( FEditFilter.Text );
  if ( Length( newFilter ) > Length( FOldFilter ) )
  and ( Pos( FOldFilter, newFilter ) = 1 ) then
    InternalRefreshList( Sender, FCurrSelection )
  else
    InternalRefreshList( Sender, FList );
  FOldFilter := Trim( FEditFilter.Text );
end;

procedure TSpotLightContext.RefreshFilter;
begin
  if not VerifyFilter then
    exit;
  FHeaderLabel.Transparent := true;
  if FList.Count <> FListBox.Count then
  begin
    FSmallLabel.Font.Color := clBlack;
    FSmallLabel.Caption := Format('%d av %d', [FListBox.Count, FList.Count]);
    FHeaderPanel.Color := clWebOrange;
    FHeaderLabel.Font.Color := clBlack;
    FHeaderLabel.Update;
  end
  else
  begin
    FSmallLabel.Font.Color := clWhite;
    FSmallLabel.Caption := Format('n = %d', [FListBox.Count]);
    FHeaderPanel.Color := GlobalStyle.BaseColor;
    FHeaderLabel.Font.Color := clWhite;
    FHeaderLabel.Update;
  end;
end;

procedure TSpotLightContext.InternalRefreshList(Sender: TObject; AList: TList );
var
  n: integer;
  thisItem: IListBoxBase;
  savedItem: TObject;
  showSimple, showAll: boolean;
  lookFor, itemText: string;
begin
  FListBox.Items.BeginUpdate;
  if Assigned( FList ) then
  try
    if AList = FList then
      FCurrSelection.Clear;
    if FListBox.ItemIndex <> -1 then
      savedItem := FListBox.Items.Objects[FListBox.ItemIndex]
    else
      savedItem := nil;
    if Assigned( FProgressBar ) then
    begin
      FProgressBar.Visible := true;
      FProgressBar.Max := AList.Count;
    end;
    if Assigned( FEditFilter ) then
      lookFor := AnsiUppercase(Trim(FEditFilter.Text))
    else
      lookFor := EmptyStr;
    if Assigned( FSimpleView ) then
      showSimple := FSimpleView.Checked
    else
      showSimple := false;
    if Assigned( FShowAll ) then
      showAll := FShowAll.Checked
    else
      showAll := true;
    FListBox.Clear;
    if Assigned(AList) and (AList.Count > 0) then
    begin
      n := 0;
      while n < AList.Count do
      try
        if Supports(AList[n], IListBoxBase, thisItem) then
        begin
          itemText := thisItem.AsListbox(showSimple);
          if (lookFor = EmptyStr) or (Pos(lookFor, AnsiUppercase(itemText)) > 0) then
          begin
            if (showAll) or thisItem.IsCurrent then
            begin
              FListBox.Items.AddObject(itemText, thisItem as TObject );
              if AList <> FCurrSelection then
                FCurrSelection.Add( thisItem as TObject );
            end;
          end;
        end;
        inc(n);
        if Assigned( FProgressBar ) and ( n mod 10 = 0 ) then
          FProgressBar.Position := n;
      except on E:Exception do
        begin
          Log.SilentError( '%s.InternalRefreshList(%d): %s', [ClassName,n,E.Message] );
          raise;
        end;
      end;
      if Assigned( FProgressBar ) then
        FProgressBar.Visible := false;
      if savedItem <> nil then
        FListBox.ItemIndex := FListBox.Items.IndexOfObject(savedItem);
    end;
    RefreshFilter;
  finally
    FListBox.Items.EndUpdate;
  end;
end;

procedure TSpotLightContext.SetHeader(AHeaderPanel: TPanel; AHeaderLabel, ASmallLabel: TLabel);
begin
  FHeaderPanel := AHeaderPanel;
  FHeaderLabel := AHeaderLabel;
  FSmallLabel := ASmallLabel;
end;

procedure TSpotLightContext.RefreshList( Sender: TObject );
begin
  InternalRefreshList( Sender, FList );
end;

end.
