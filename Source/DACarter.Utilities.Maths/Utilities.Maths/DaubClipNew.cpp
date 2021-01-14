////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: DaubClip
//
// DESCRIPTION: 
// Perform the Daubechies Clip Wavelet algorithm
// 1) Unlace the I and Q data into two separate buffers. 
//    (i.e. I,Q,I,Q,I,Q,I,Q... TO-> I,I,I,I AND-> Q,Q,Q,Q. 
//    Remove DC from the data while doing this.
// 2) Perform a daub20 wavelet transform on the I and the Q buffers. 
// 3) Determine the threshold
// 4) Clip all values larger than a set threshold to that threshold value. 
// 4) Reconstruct the time series array.
// 5) Replace the I's & Q's into the outgoing LapxmDataStucture.
//
// INPUT/OUTPUT: LapxmDataStructure *pLapxmData,                          
//
// OUTPUT: bool *pbSizeUnfit - indicates if array size is unfit
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 11/1/2013 
//
// LAST MODIFIED BY:
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::DaubClip(LapxmDataStructure *pLapxmData, bool *pbSizeUnfit)
{
  
  // Get parameters from incoming lapxm data structure.
  int iNRec  = pLapxmData->pBeam[0].Pulse.lNumReceivers;
  int iNGate = pLapxmData->pBeam[0].Pulse.lNumRangeGates;
  int iNpts  = pLapxmData->pBeam[0].Dwell.lNumPointsInSpectrum * pLapxmData->pBeam[0].Dwell.lNumSpectralInAverage;
  int iNpts_PowerOfTwo = iNpts;
  float fMaxVelocity = pLapxmData->pBeam[0].Dwell.fNyquistFrequencyMps;


  // Reject array sizes less then 1024
  if (iNpts < 1024)
  {
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr("DaubClip algorithm requires iNpts * iNSpec to be equal or greater than 1024. DaubClip will be ignored for input data ") + CBstr((char*)pLapxmData->cDataProductName));
    *pbSizeUnfit = true;
    return E_FAIL;
  }
	
	// Calculate Order - iNpts must be rounded up to the next power of 2.
	int iOrder =(int)ceil(log((double)iNpts)/log(2.0));  
  iNpts_PowerOfTwo = (int)pow((double)2,iOrder);

  // Calculate number of points to process
  // We only process the points from zero veloctiy up to 2.5ms since this is where clutter will be found
  int iLevel = (int)(fMaxVelocity / 5.0); // 5 = 2.5ms * 2 where 2 is the smooth half of the wavelet transformation
  int iNumPointsInSegment = (int)pow((double)2,(iOrder - iLevel));
  int iNumOfZerosInSegment = (iNpts_PowerOfTwo - iNpts) / (int)pow((double)2,(iLevel));


  // Select the parameters requested for this data product.
  double dThresholdRatio_DaubClip = 4.5;

  // Allocate space to hold I and Q data points along with zero padding to bring the total up to an even power of 2.
	double *pDataI = new double[iNpts_PowerOfTwo];
	double *pDataQ = new double[iNpts_PowerOfTwo];

  // Loop through every receiver.
  for (int iRec = 0; iRec < iNRec; iRec++)
  {
    int iReceiverOffset = iRec*iNGate*iNpts*2;

    // Loop through every gate.
    for (int iGate = 0; iGate < iNGate; iGate++)
    {
      // Calculate the first point at this gate.
      int iPointOffSet = iReceiverOffset + 2*iGate*iNpts; 

      // Calculate the average for I and Q
      double dI_Average = 0.0; 
      double dQ_Average = 0.0; 
      for (int iPoint = 0; iPoint < iNpts; iPoint++)
      {
        dI_Average += pLapxmData->pData[iPointOffSet + (2 * iPoint)];
        dQ_Average += pLapxmData->pData[iPointOffSet + (2 * iPoint) + 1];
      }
      dI_Average = dI_Average / iNpts;
      dQ_Average = dQ_Average / iNpts;

      // Copy data into temporary buffers to unlace the Is & Qs into two seperate blocks. I,Q,I,Q,I,Q.... TO-> I,I,I,I.... AND-> Q,Q,Q,Q.....
      // Remove the DC (average) from I and Q separately.
      for (int iPoint = 0; iPoint < iNpts; iPoint++)
      {
        pDataI[iPoint] = pLapxmData->pData[iPointOffSet + (2 * iPoint)] - dI_Average;
        pDataQ[iPoint] = pLapxmData->pData[iPointOffSet + (2 * iPoint) + 1] - dQ_Average;
      }

      // Add zeros to pad the data to the full size of an even power of 2.
      for (int iPoint = iNpts; iPoint < iNpts_PowerOfTwo; iPoint++)
      {
        pDataI[iPoint] = 0.0;
        pDataQ[iPoint] = 0.0;
      }

      // Wavelet decomposition using Daubechies20.
      DiscreteWaveletTransform(pDataI, iNpts_PowerOfTwo, 1, "daub20", iGate);
      DiscreteWaveletTransform(pDataQ, iNpts_PowerOfTwo, 1, "daub20", iGate);

      // Create buffer to hold data for sorting
      int iNumPointsInBuffer = iNumPointsInSegment * 2;
      double *pBuffer = new double[iNumPointsInBuffer];

      // Move I + Q data into one buffer and change to absolute values
      for (int iPoint = 0; iPoint < iNumPointsInSegment; iPoint++)
      {
        pBuffer[iPoint] = fabs(pDataI[iPoint]);
        pBuffer[iNumPointsInSegment + iPoint] = fabs(pDataQ[iPoint]);
      }

      // Sort from smallest to largest
      qsort((void *)pBuffer, (size_t)(iNumPointsInBuffer), (size_t)sizeof(double), compare_double);

      // Find value of the median point. Exclude the zero padding which will show up at the beginning of the array
      int iMedianPoint = (2 * iNumOfZerosInSegment) + ((iNumPointsInBuffer - (iNumOfZerosInSegment * 2)) / 2);
      double dMedian = pBuffer[iMedianPoint];

      // Determine Threshold Value
      // Loop from the last point to the first point until the ratio of the point divided by the meadian point <= dThresholdRatio_DaubClip
      double dThreshold = pBuffer[iNumPointsInBuffer -1];
      for (int iPoint = iNumPointsInBuffer -1; iPoint >= 0; iPoint--)
      {
        if ((pBuffer[iPoint] / dMedian) <= dThresholdRatio_DaubClip)
        {
          break;
        }
        else
        {
          dThreshold = pBuffer[iPoint];
        }
      }
      delete [] pBuffer;

      // Clip all values to a maximum of a set threshold.
      for (int iPoint = 0; iPoint < iNumPointsInSegment; iPoint++)
      {
        // Clip the Is
        if (fabs(pDataI[iPoint]) > dThreshold)
        {
          // Restore the sign 
          pDataI[iPoint] = pDataI[iPoint]/fabs(pDataI[iPoint]) * dThreshold;
        }

        // Clip the Qs
        if (fabs(pDataQ[iPoint]) > dThreshold)
        {
          // Restore the sign 
          pDataQ[iPoint] = pDataQ[iPoint]/fabs(pDataQ[iPoint]) * dThreshold;
        }
      }

      // Inverse Wavelet transform.
      DiscreteWaveletTransform(pDataI, iNpts_PowerOfTwo, -1, "daub20", iGate);
      DiscreteWaveletTransform(pDataQ, iNpts_PowerOfTwo, -1, "daub20", iGate);

       // Copy data out of the temp buffer and relace them into the lapxm data structure
      // I,Q,I,Q,I,Q......
      for (int iPoint = 0; iPoint < iNpts; iPoint++)
      {
        pLapxmData->pData[iPointOffSet + (2 * iPoint)] = (float)pDataI[iPoint];
        pLapxmData->pData[iPointOffSet + (2 * iPoint) + 1] = (float)pDataQ[iPoint];
      }

    }

  }
  delete [] pDataI;
  delete [] pDataQ;

  return S_OK;
}

////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: DiscreteWaveletTransform
//
// DESCRIPTION: Performs the discrete wavelet transform or inverse wavelet
// transform.
//
// INPUT/OUTPUT: double *pdData - data array to be transformed
//
// INPUT: 
// int iLength   - length of *pdData
// int iMethod   - transform methods: 1: full transform -1: full inverse transform
//                 2: transform, only largest hierarchy level 
//                 -2: inverse transform, only largest hierarchy level
// char *cFilter - filter name "haar" or "daub20"
// int iGate     - current range gate, for log file printing
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Raisa Lehtinen 2005-01-11
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT 
CCustomWorkerThread::DiscreteWaveletTransform(double *pdData, // data array I/O
                                              int iLength,    // length of data
                                              int iMethod,    // 1,2 = transform -1,-2 = inverse tr.
                                              char *cFilter,  // name of wavelet filter
                                              int iGate)      // current range gate
{
  
  int iSmallestLevel = 4;
  if (iMethod == -2 || iMethod == 2)
  {
    iSmallestLevel = iLength;
  }

  int iNCoeff;
  std::vector<double> vecCC; // coefficients of the smoothing filter
  std::vector<double> vecCR; // coefficients of the wavelet function
 
  if (strcmp(cFilter, "haar") == 0) // Haar wavelet = Daubechies 2
  {
    iNCoeff = 2;
    vecCC.resize(iNCoeff);
    vecCC[0] = 0.70710678118655;
    vecCC[1] = 0.70710678118655;
    vecCR.resize(iNCoeff);
    vecCR[0] = 0.70710678118655;
    vecCR[1] = -0.70710678118655;
  }
  else if (strcmp(cFilter, "daub20") == 0) // Daubechies filter with 20 coefficients.
  {
    iNCoeff = 20;

    // Set smooth coefficients
    vecCC.resize(iNCoeff);
    vecCC[0]  =  0.026670057901; 
    vecCC[1]  =  0.188176800078; 
    vecCC[2]  =  0.527201188932;
    vecCC[3]  =  0.688459039454; 
    vecCC[4]  =  0.281172343661; 
    vecCC[5]  = -0.249846424327;
    vecCC[6]  = -0.195946274377; 
    vecCC[7]  =  0.127369340336; 
    vecCC[8]  =  0.093057364604;
    vecCC[9]  = -0.071394147166; 
    vecCC[10] = -0.029457536822; 
    vecCC[11] =  0.033212674059;
    vecCC[12] =  0.003606553567; 
    vecCC[13] = -0.010733175483; 
    vecCC[14] =  0.001395351747;
    vecCC[15] =  0.001992405295; 
    vecCC[16] = -0.000685856695; 
    vecCC[17] = -0.000116466855;
    vecCC[18] =  0.000093588670; 
    vecCC[19] = -0.000013264203;

    // Set details coefficients
    double dMult = -1.0;
    vecCR.resize(iNCoeff);
    for (int i = 0; i < iNCoeff; i++)
    {
      vecCR[(iNCoeff-1)-i] = dMult * vecCC[i];
      dMult = -dMult;
    }
  }
  else
  {
    return E_FAIL; // filter name invalid.
  }
  
  int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
  int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
  
  std::vector<double> vecWksp;
  int iNMod, iNN1, iNH;
  int iNI, iNJ, iJF, iJR;
  double dAi, dAi1;
  
  // Wavelet transform
  if (iMethod >= 0)  
  { 
    // Decompose

    // Loop through each level - (power of two 8192, 4096, 2048, etc)
    for (int iNN = iLength; iNN >= iSmallestLevel; iNN >>= 1)
    {
      vecWksp.assign(iNN, 0.0);
      iNMod = iNCoeff * iNN;
      iNN1 = iNN - 1;
      iNH = iNN >> 1;
      for (int ii = 0, i = 0; i < iNN; i += 2, ii++)
      {
        iNI = i+1+iNMod+iIoff;
        iNJ = i+1+iNMod+iJoff; 
        for (int k = 0; k < iNCoeff; k++)
        {
          iJF = iNN1 & (iNI+k+1);
          iJR = iNN1 & (iNJ+k+1);
          vecWksp[ii] += vecCC[k] * pdData[iJF];
          vecWksp[ii+iNH] += vecCR[k] * pdData[iJR];
        }
      }

      // Write over data with new results
      // The lower half contains the details. The upper half contain the smooth values.
      // When performing multiple levels, the lower half from the previous level is over written with the new data.
      for (int i = 0; i < iNN; i++)
      {
        pdData[i] = vecWksp[i];
      }
    }
  }
  else 
  { 
    // Inverse

    // Loop through each level - (power of two 8192, 4096, 2048, etc)
    for (int iNN = iSmallestLevel; iNN <= iLength; iNN <<= 1)
    {
      vecWksp.assign(iNN, 0.0);
      iNMod = iNCoeff * iNN;
      iNN1 = iNN - 1;
      iNH = iNN >> 1;
      for (int ii = 0, i = 0; i < iNN; i += 2, ii++)
      {
        dAi = pdData[ii];
        dAi1 = pdData[ii+iNH];
        iNI = i+1+iNMod+iIoff;
        iNJ = i+1+iNMod+iJoff; 
        for (int k = 0; k < iNCoeff; k++)
        {
          iJF = iNN1 & (iNI+k+1);
          iJR = iNN1 & (iNJ+k+1);
          vecWksp[iJF] += vecCC[k] * dAi;
          vecWksp[iJR] += vecCR[k] * dAi1;
        }
      }

      // Write over data with new results
      // The lower half contains the details. The upper half contain the smooth values.
      // When performing multiple levels, the lower half from the previous level is over written with the new data.
      for (int i = 0; i < iNN; i++)
      {
        pdData[i] = vecWksp[i];
      }
    }
  }
    
  return S_OK;
}

