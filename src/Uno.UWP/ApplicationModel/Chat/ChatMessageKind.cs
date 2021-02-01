namespace Windows.ApplicationModel.Chat
{
	public   enum ChatMessageKind 
	{
		Standard,
		FileTransferRequest,
		TransportCustom,
		JoinedConversation,
		LeftConversation,
		OtherParticipantJoinedConversation,
		OtherParticipantLeftConversation,
	}
}
