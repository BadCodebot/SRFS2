#pragma once
#include "Vector.h"

namespace ReedSolomon {

	class Matrix {

	public:

		Matrix(int rows, int columns);
		Matrix(const Matrix& m);

		virtual ~Matrix();

		Matrix& operator=(const Matrix& m);

		inline const Vector& operator[](int r) const { return *matrix[r]; }

		inline Vector& operator[](int r) { return *matrix[r]; }

		inline int GetRows() const { return rows; }

		inline int GetColumns() const { return columns; }

		void Print() const;

	protected:

		Vector** matrix;

	private:

		int rows;
		int columns;
	};

	Matrix operator*(const Matrix& a, const Matrix& b);
	Vector operator*(const Matrix& m, const Vector& v);
	Vector operator*(const Vector& v, const Matrix& m);
}