<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Trunked._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <script>
        function showLoadingGif() {
            $("#imgLoading").show();
            $('#<%= lblStatus.ClientID %>').html("");
            $('#<%= tblResults.ClientID %>').css({ 'display': "none" });
            $('#<%= tblObjectResults.ClientID %>').css({ 'display': "none" });
            $('#<%= lblRecognizedAs.ClientID %>').css({ 'display': "none" });
        }

        function showSmallLoadingGif() {
            $("#imgSmallLoading").show();
        }
	</script>
    <div class="jumbotron">
        <h1>Trunked</h1>
        <asp:Panel ID="pnlRecognition" runat="server" >
            <p class="lead">Choose your image below and then click <strong><i>Recognize</i></strong>.</p>
		    <p><asp:FileUpload ID="ctrlFileUpload" runat="server" /></p>

            <p>
			    <asp:Button ID="btnRecognize" CssClass="btn btn-primary btn-lg" runat="server" OnClientClick="showLoadingGif()" OnClick="btnRecognize_Click" Text="Recognize" />
			    <img id="imgLoading" src="Content/Images/Loading.gif" style="width:30px;display:none;"/>
		    </p>	
            <asp:Button ID="TEST" runat="server" OnClick="btnBookNotFound_Click" Text="TEST" />
		    <asp:Label ID="lblStatus" runat="server" style="color: firebrick;" Visible="false" />

            <asp:Panel ID="pnlRecognizedAs" runat="server" Visible="false" >
                <p>
                    <asp:Label ID="lblRecognizedAs" runat="server" />
                    <br />
                    <asp:Label runat="server" style="font-size: 16px !important;" Text="If that's not correct; upload a different image or " />
                    <asp:LinkButton ID="lnkbtnManualInput" runat="server" style="font-size: 16px !important;" Text="click here" OnClick="lnkbtnManualInput_Click" />
                    <asp:Label runat="server" Text=" to add the item manually" style="font-size: 16px !important;" />
                    <br /><br />
                </p>
            </asp:Panel>

            <asp:Button ID="btnBookNotFound" runat="server" OnClick="btnBookNotFound_Click" CssClass="btn btn-primary" Text="Book not here?" Visible="false" />
            <asp:Label ID="lblNewLines" runat="server" Text="<br /><br />" Visible="false" />

		    <asp:Table ID="tblObjectResults" runat="server" GridLines="Both" Visible="false" >
                <asp:TableRow>
                    <asp:TableCell Text="Item scanned" CssClass="tblCell heading" />
                    <asp:TableCell Text="Confidence" CssClass="tblCell heading" />
                    <asp:TableCell Text="Confirmation" CssClass="tblCell heading" />
                </asp:TableRow>
                <asp:TableRow>
                    <asp:TableCell ID="cllItemScanned" CssClass="tblCell" />
                    <asp:TableCell ID="cllConfidence" CssClass="tblCell" />
                    <asp:TableCell CssClass="tblCell">
                        <asp:Button ID="btnConfirm" runat="server" Text="Confirm" OnClick="btnConfirm_Click" CssClass="btn btn-primary" style="vertical-align: middle;" />
                    </asp:TableCell>
                </asp:TableRow>
		    </asp:Table>            
        </asp:Panel>            
        <asp:Panel ID="pnlManual" runat="server" Visible="false" >
            <p class="lead">Please fill out the form below as much as possible.</p>
            <asp:UpdatePanel ID="upnlManualForm" runat="server">
               <ContentTemplate>
                    <asp:Table ID="tblManualForm" runat="server" GridLines="None" >
                        <asp:TableRow>
                            <asp:TableCell Text="Item Type:" CssClass="tblCell heading" />
                            <asp:TableCell>
                                <asp:DropDownList ID="ddlItemType" runat="server" />
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow>
                            <asp:TableCell Text="ISBN:" CssClass="tblCell heading" />
                            <asp:TableCell>
                                <asp:TextBox ID="txtISBN" placeholder="ISBN" runat="server" AutoPostBack="true" OnTextChanged="txtISBN_TextChanged" AutoCompleteType="Disabled" /><br />
                                <asp:Panel ID="pnlSuggestion" runat="server" Visible="false" >
                                    <asp:Label ID="lblBookSuggestion" runat="server" />
                                    &nbsp;
                                    <asp:Button ID="btnYes" runat="server" CssClass="btn btn-primary" Text="Yes" OnClientClick="showSmallLoadingGif()" OnClick="btnYes_Click" style="padding-top: 2px !important;padding-bottom: 2px !important;padding-left: 2px !important;padding-right: 2px !important;" />
                                    &nbsp;
                                    <asp:Button ID="btnNo" runat="server" CssClass="btn btn-primary" Text="No" OnClick="btnNo_Click" style="padding-top: 2px !important;padding-bottom: 2px !important;padding-left: 5px !important;padding-right: 5px !important;" />
                                    <img id="imgSmallLoading" src="Content/Images/Loading.gif" style="width:5px;display:none;"/>
                                </asp:Panel>
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow>
                            <asp:TableCell Text="Title:" CssClass="tblCell heading" />
                            <asp:TableCell>
                                <asp:TextBox ID="txtTitle" placeholder="Title" runat="server" style="max-width: 500px !important;" Width="500" />
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow>
                            <asp:TableCell Text="Author(s):" CssClass="tblCell heading" />
                            <asp:TableCell>
                                <asp:TextBox ID="txtAuthors" placeholder="Author(s)" runat="server" style="max-width: 500px !important;" Width="500" />
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow>
                            <asp:TableCell Text="Publisher:" CssClass="tblCell heading" />
                            <asp:TableCell>
                                <asp:TextBox ID="txtPublisher" placeholder="Publisher" runat="server" style="max-width: 500px !important;" Width="500" />
                            </asp:TableCell>
                        </asp:TableRow>
                        <asp:TableRow>
                            <asp:TableCell>
                                <asp:Button ID="btnManualSubmit" runat="server" CssClass="btn btn-primary btn-lg" OnClick="btnManualSubmit_Click" Text="Submit" />
                            </asp:TableCell>
                        </asp:TableRow>
                    </asp:Table>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:Panel>
        <asp:Panel ID="pnlConfirmation" runat="server" Visible="false" >
            <p class="lead">Please confirm the following is correct.</p>            
        </asp:Panel>    
        <asp:Table ID="tblResults" runat="server" GridLines="Both" Visible="false" />
    </div>
</asp:Content>
