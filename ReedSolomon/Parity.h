#pragma once
#include <cstdint>
#include "Generator.h"
#include <immintrin.h>
#include "GF16MultiplicationTable.h"

namespace ReedSolomon {

	class Parity {

	public:

		Parity(size_t nDataCodewords, size_t nParityCodewords, size_t codewordsPerSlice);
		~Parity();

		void Reset();
		void Calculate(char* data, size_t codewordIndex);

		inline size_t GetNParityCodewords() const { return _nParityCodewords; }
		inline size_t GetNParityBlocks() const { return _parityBlocksPerVector; }
		inline __m128i* GetFirstParityBlock() const { return (__m128i*)_parityVectors; }

	private:

		size_t _nParityCodewords;
		size_t _parityBlocksPerVector;
		size_t _nDataCodewords;
		size_t _codewordsPerSlice;

		Generator _generator;
		GF16MultiplicationTable _multiplicationTable;

		uint16_t* _parityVectors;
		__m128i* _parity;
	};
}
