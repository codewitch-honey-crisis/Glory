namespace Glory
{
#if CFGLIB
	public
#endif
	enum CfgLRStatus
	{
		Unknown=0,
		ComputingClosure,
		ComputingStates,
		ComputingConfigurations,
		ComputingMove,
		CreatingLRExtendedGrammar,
		ComputingReductions
	}
#if CFGLIB
	public
#endif
	struct CfgLRProgress
	{
		public readonly CfgLRStatus Status;
		public readonly int Count;
		public CfgLRProgress(CfgLRStatus status, int count)
		{
			Status = status;
			Count = count;
		}
		
	}
}
