unit Unit1;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls, mcl_pm_TLB ;

type
  TForm1 = class(TForm)
    Label1: TLabel;
    Edit1: TEdit;
    Label2: TLabel;
    Label3: TLabel;
    Button1: TButton;
    Button2: TButton;
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
  private
    { Private declarations }
    StopRun: boolean ;
  public
    { Public declarations }
  end;

var
  Form1: TForm1;

implementation

{$R *.DFM}


procedure TForm1.Button1Click(Sender: TObject);
var
  MyPM : _USB_PM ;
  Result: real;
  status: longint;
begin
 MyPm := cousb_pm.Create;// (_USB_PM)     //  TUSB_PM.Create(self) ;//(self);
 status:=MyPM.Open_AnySensor ;


 StopRun :=false ;
while (not StopRun)
do begin
 MyPM.Freq := strtofloat (edit1.text);
 Result:=MyPM.ReadPower;
 label3.Caption:=format('%4.2f dBm',[Result]);
  Application.ProcessMessages ;


end ;



end;

procedure TForm1.Button2Click(Sender: TObject);
begin
StopRun:=true;
end;

end.
