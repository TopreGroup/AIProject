<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Trunked._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="jumbotron">
        <h1>Trunked</h1>
        <p class="lead">Upload your image below and then click the  <strong>"Recognize"</strong> button.</p>
		<p><asp:FileUpload ID="ctrlFileUpload" runat="server" /></p>

		<br />

		<asp:Label ID="lblStatus" runat="server" Visible="false" Text="Upload status: " />

        <p>
			<asp:Button ID="btnRecognize" CssClass="btn btn-primary btn-lg" runat="server" OnClientClick="showLoadingGif()" OnClick="RecognizeButton_Click" Text="Recognize" />
			<img id="imgLoading" src="Content/Images/Loading.gif" style="width:30px;display:none;"/>
		</p>	

        <p><asp:Label ID="lblResults" runat="server" /></p>
        <br />

		<asp:Table ID="tblResults" runat="server" GridLines="Both" Visible="false" />
    </div>
</asp:Content>
