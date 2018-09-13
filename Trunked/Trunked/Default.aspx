﻿<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Trunked._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <script>
		function showLoadingGif() {
			$("#imgLoading").show();
        }

        function hideLoadingGif() {
			$("#imgLoading").hide();
		}
	</script>
    <div class="jumbotron">
        <h1>Trunked</h1>
        <p class="lead">Choose your image below and then click the  <strong>"Recognize"</strong> button.</p>
		<p><asp:FileUpload ID="ctrlFileUpload" runat="server" /></p>

        <p>
			<asp:Button ID="btnRecognize" CssClass="btn btn-primary btn-lg" runat="server" OnClientClick="showLoadingGif()" OnClick="RecognizeButton_Click" Text="Recognize" />
			<img id="imgLoading" src="Content/Images/Loading.gif" style="width:30px;display:none;"/>
		</p>	

		<p><asp:Label ID="lblStatus" runat="server" Visible="false" Text="Upload status: " /></p><br />

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
                    <asp:Button ID="btnConfirm" runat="server" Text="Confirm" CssClass="btn btn-primary" style="vertical-align: middle;" />
                </asp:TableCell>
            </asp:TableRow>
		</asp:Table>
    </div>
</asp:Content>
